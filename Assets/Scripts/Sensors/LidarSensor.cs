/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Lidar", new[] { typeof(PointCloudData) })]
    public partial class LidarSensor : SensorBase
    {
        // Lidar x is forward, y is left, z is up
        public static readonly Matrix4x4 LidarTransform = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero);

        [HideInInspector]
        public int TemplateIndex;

        int CurrentLaserCount;
        int CurrentMeasurementsPerRotation;
        float CurrentFieldOfView;
        List<float> CurrentVerticalRayAngles;
        float CurrentCenterAngle;
        float CurrentMinDistance;
        float CurrentMaxDistance;

        // Horizontal FOV of the camera
        const float HorizontalAngleLimit = 15.0f;

        // List vertical angles for each ray.
        //
        // For models with uniformly distributed vertical angles,
        // this list should be empty (i.e. Count == 0).
        // For models with non-uniformly distributed vertical angles,
        // this Count of this list should equals to LaserCount.
        // Angle values follows Velodyne's convetion:
        // 0 means horizontal;
        // Positive means tilting up; 
        // Negative means tilting down.
        //
        // Refer to LidarSensorEditor.cs for more details of the relationship
        // between VerticalRayAngles and LaserCount/FieldOfView/CenterAngle.
        [SensorParameter]
        public List<float> VerticalRayAngles;

        [SensorParameter]
        [Range(1, 128)]
        public int LaserCount = 32;

        [SensorParameter]
        [Range(1.0f, 45.0f)]
        public float FieldOfView = 40.0f;

        [SensorParameter]
        [Range(-45.0f, 45.0f)]
        public float CenterAngle = 10.0f;

        [SensorParameter]
        [Range(0.01f, 1000f)]
        public float MinDistance = 0.5f; // meters

        [SensorParameter]
        [Range(0.01f, 2000f)]
        public float MaxDistance = 100.0f; // meters

        [SensorParameter]
        [Range(1, 30)]
        public float RotationFrequency = 5.0f; // Hz

        [SensorParameter]
        [Range(18, 6000)] // minmimum is 360/HorizontalAngleLimit
        public int MeasurementsPerRotation = 1500; // for each ray

        [SensorParameter]
        public bool Compensated = true;
        
        public GameObject Top = null;

        [SensorParameter]
        [Range(1, 10)]
        public float PointSize = 2.0f;

        [SensorParameter]
        public Color PointColor = Color.red;

        IBridge Bridge;
        IWriter<PointCloudData> Writer;
        uint SendSequence;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Vector4> Points;

        ComputeBuffer PointCloudBuffer;
        int PointCloudLayer;

        Material PointCloudMaterial;

        private Camera Camera;
        private bool updated;
        private NativeArray<float> SinLatitudeAngles;
        private NativeArray<float> CosLatitudeAngles;

        struct ReadRequest
        {
            public RenderTexture RenderTexture;
            public AsyncGPUReadbackRequest Readback;
            public int Index;
            public int Count;
            public Vector3 Origin;

            public Matrix4x4 Transform;
            public Matrix4x4 CameraToWorldMatrix;
        }

        List<ReadRequest> Active = new List<ReadRequest>();
        List<JobHandle> Jobs = new List<JobHandle>();

        Stack<RenderTexture> AvailableRenderTextures = new Stack<RenderTexture>();
        Stack<Texture2D> AvailableTextures = new Stack<Texture2D>();

        int CurrentIndex;
        float AngleStart;
        float AngleDelta;

        float MaxAngle;
        int RenderTextureWidth;
        int RenderTextureHeight;
        float SinStartLongitudeAngle;
        float CosStartLongitudeAngle;
        float SinDeltaLongitudeAngle;
        float CosDeltaLongitudeAngle;

        // Scales between world coordinates and texture coordinates
        float XScale;
        float YScale;
        
        float IgnoreNewRquests;

        ProfilerMarker UpdateMarker = new ProfilerMarker("Lidar.Update");
        ProfilerMarker VisualizeMarker = new ProfilerMarker("Lidar.Visualzie");
        ProfilerMarker BeginReadMarker = new ProfilerMarker("Lidar.BeginRead");
        ProfilerMarker EndReadMarker = new ProfilerMarker("Lidar.EndRead");

        public void ApplyTemplate()
        {
            var values = Template.Templates[TemplateIndex];
            LaserCount = values.LaserCount;
            MinDistance = values.MinDistance;
            MaxDistance = values.MaxDistance;
            RotationFrequency = values.RotationFrequency;
            MeasurementsPerRotation = values.MeasurementsPerRotation;
            FieldOfView = values.FieldOfView;
            VerticalRayAngles = new List<float>(values.VerticalRayAngles);
            CenterAngle = values.CenterAngle;
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<PointCloudData>(Topic);
        }

        public void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var camera = hd.camera;

            ScriptableCullingParameters culling;
            if (camera.TryGetCullingParameters(out culling))
            {
                var cull = context.Cull(ref culling);

                context.SetupCameraProperties(camera);

                var cmd = CommandBufferPool.Get();
                hd.SetupGlobalParams(cmd, 0, 0, 0);
                cmd.ClearRenderTarget(true, true, Color.black);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorLidarPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }

        public void Init()
        {
            Camera = GetComponentInChildren<Camera>();
            Camera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;
            PointCloudMaterial = new Material(RuntimeSettings.Instance.PointCloudShader);
            PointCloudLayer = LayerMask.NameToLayer("Sensor Effects");

            Reset();
        }

        private void Start()
        {
            Init();
        }

        private float CalculateFovAngle(float latitudeAngle, float logitudeAngle)
        {
            // Calculate a direction (dx, dy, dz) using lat/log angles
            float dy = Mathf.Cos(latitudeAngle * Mathf.Deg2Rad);
            float rProjected = Mathf.Sin(latitudeAngle * Mathf.Deg2Rad);
            float dz = rProjected * Mathf.Sin(logitudeAngle * Mathf.Deg2Rad);
            float dx = rProjected * Mathf.Cos(logitudeAngle * Mathf.Deg2Rad);

            // Project the driection to near plane
            float projectionScale = MinDistance / dz;
            float xx = dx * projectionScale;
            float yy = dy * projectionScale;

            return Mathf.Abs(Mathf.Atan2(yy, MinDistance) * Mathf.Rad2Deg);
        }

        public void Reset()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.RenderTexture.Release();
            });
            Active.Clear();

            Jobs.ForEach(job => job.Complete());
            Jobs.Clear();

            foreach (var tex in AvailableRenderTextures)
            {
                tex.Release();
            };
            AvailableRenderTextures.Clear();

            foreach (var tex in AvailableTextures)
            {
                Destroy(tex);
            };
            AvailableTextures.Clear();

            if (PointCloudBuffer != null)
            {
                PointCloudBuffer.Release();
                PointCloudBuffer = null;
            }

            if (Points.IsCreated)
            {
                Points.Dispose();
            }

            AngleStart = 0.0f;
            // Assuming center of view frustum is horizontal, find the vertical FOV (of view frustum) that can encompass the tilted Lidar FOV.
            // "MaxAngle" is half of the vertical FOV of view frustum.
            float startLatitudeAngle, endLatitudeAngle;
            if (VerticalRayAngles.Count == 0)
            {
                MaxAngle = Mathf.Abs(CenterAngle) + FieldOfView / 2.0f;

                startLatitudeAngle = 90.0f + MaxAngle;
                //If the Lidar is tilted up, ignore lower part of the vertical FOV.
                if (CenterAngle < 0.0f)
                {
                    startLatitudeAngle -= MaxAngle * 2.0f - FieldOfView;
                }
                endLatitudeAngle = startLatitudeAngle - FieldOfView;
            }
            else
            {
                LaserCount = VerticalRayAngles.Count;
                startLatitudeAngle = 90.0f - VerticalRayAngles.Min();
                endLatitudeAngle = 90.0f - VerticalRayAngles.Max();
                FieldOfView = startLatitudeAngle - endLatitudeAngle;
                MaxAngle = Mathf.Max(startLatitudeAngle - 90.0f, 90.0f - endLatitudeAngle);
            }

            float startLongitudeAngle = 90.0f + HorizontalAngleLimit / 2.0f;
            SinStartLongitudeAngle = Mathf.Sin(startLongitudeAngle * Mathf.Deg2Rad);
            CosStartLongitudeAngle = Mathf.Cos(startLongitudeAngle * Mathf.Deg2Rad);

            // The MaxAngle above is the calculated at the center of the view frustum.
            // Because the scan curve for a particular laser ray is a hyperbola (intersection of a conic surface and a vertical plane),
            // the vertical FOV should be enlarged toward left and right ends.
            float startFovAngle = CalculateFovAngle(startLatitudeAngle, startLongitudeAngle);
            float endFovAngle = CalculateFovAngle(endLatitudeAngle, startLongitudeAngle);
            MaxAngle = Mathf.Max(MaxAngle, Mathf.Max(startFovAngle, endFovAngle));

            // Calculate sin/cos of latitude angle of each ray.
            if (SinLatitudeAngles.IsCreated)
            {
                SinLatitudeAngles.Dispose();
            }
            if (CosLatitudeAngles.IsCreated)
            {
                CosLatitudeAngles.Dispose();
            }
            SinLatitudeAngles = new NativeArray<float>(LaserCount, Allocator.Persistent);
            CosLatitudeAngles = new NativeArray<float>(LaserCount, Allocator.Persistent);
            // If VerticalRayAngles array is not provided, use uniformly distributed angles.
            if (VerticalRayAngles.Count == 0)
            {
                float deltaLatitudeAngle = FieldOfView / LaserCount;
                int index = 0;
                float angle = startLatitudeAngle;
                while (index < LaserCount)
                {
                    SinLatitudeAngles[index] = Mathf.Sin(angle * Mathf.Deg2Rad);
                    CosLatitudeAngles[index] = Mathf.Cos(angle * Mathf.Deg2Rad);
                    index++;
                    angle -= deltaLatitudeAngle;
                }
            }
            else
            {
                for (int index = 0; index < LaserCount; index++)
                {
                    SinLatitudeAngles[index] = Mathf.Sin((90.0f - VerticalRayAngles[index]) * Mathf.Deg2Rad);
                    CosLatitudeAngles[index] = Mathf.Cos((90.0f - VerticalRayAngles[index]) * Mathf.Deg2Rad);
                }
            }

            int count = (int)(HorizontalAngleLimit / (360.0f / MeasurementsPerRotation));
            float deltaLongitudeAngle = (float)HorizontalAngleLimit / (float)count;
            SinDeltaLongitudeAngle = Mathf.Sin(deltaLongitudeAngle * Mathf.Deg2Rad);
            CosDeltaLongitudeAngle = Mathf.Cos(deltaLongitudeAngle * Mathf.Deg2Rad);

            // Enlarged the texture by some factors to mitigate alias.
            RenderTextureHeight = 16 * (int)(2.0f * MaxAngle * LaserCount / FieldOfView);
            RenderTextureWidth = 8 * (int)(HorizontalAngleLimit / (360.0f / MeasurementsPerRotation));

            // View frustum size at the near plane.
            float frustumWidth = 2 * MinDistance * Mathf.Tan(HorizontalAngleLimit / 2.0f * Mathf.Deg2Rad);
            float frustumHeight = 2 * MinDistance * Mathf.Tan(MaxAngle * Mathf.Deg2Rad);
            XScale = frustumWidth / RenderTextureWidth;
            YScale = frustumHeight / RenderTextureHeight;

            // construct custom aspect ratio projection matrix
            // math from https://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/opengl-perspective-projection-matrix

            float v = 1.0f / Mathf.Tan(MaxAngle * Mathf.Deg2Rad);
            float h = 1.0f / Mathf.Tan(HorizontalAngleLimit * Mathf.Deg2Rad / 2.0f);
            float a = (MaxDistance + MinDistance) / (MinDistance - MaxDistance);
            float b = 2.0f * MaxDistance * MinDistance / (MinDistance - MaxDistance);

            var projection = new Matrix4x4(
                new Vector4(h, 0, 0, 0),
                new Vector4(0, v, 0, 0),
                new Vector4(0, 0, a, -1),
                new Vector4(0, 0, b, 0));

            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;
            Camera.projectionMatrix = projection;

            int totalCount = LaserCount * MeasurementsPerRotation;
            PointCloudBuffer = new ComputeBuffer(totalCount, UnsafeUtility.SizeOf<Vector4>());
            PointCloudMaterial?.SetBuffer("_PointCloud", PointCloudBuffer);

            Points = new NativeArray<Vector4>(totalCount, Allocator.Persistent);

            CurrentLaserCount = LaserCount;
            CurrentMeasurementsPerRotation = MeasurementsPerRotation;
            CurrentFieldOfView = FieldOfView;
            CurrentVerticalRayAngles = new List<float>(VerticalRayAngles);
            CurrentCenterAngle = CenterAngle;
            CurrentMinDistance = MinDistance;
            CurrentMaxDistance = MaxDistance;

            IgnoreNewRquests = 0;
        }

        void OnDisable()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.RenderTexture.Release();
            });
            Active.Clear();

            Jobs.ForEach(job => job.Complete());
            Jobs.Clear();
        }

        void Update()
        {
            if (LaserCount != CurrentLaserCount ||
                MeasurementsPerRotation != CurrentMeasurementsPerRotation ||
                FieldOfView != CurrentFieldOfView ||
                CenterAngle != CurrentCenterAngle ||
                MinDistance != CurrentMinDistance ||
                MaxDistance != CurrentMaxDistance ||
                !Enumerable.SequenceEqual(VerticalRayAngles, CurrentVerticalRayAngles))
            {
                if (MinDistance > 0 && MaxDistance > 0 && LaserCount > 0 && MeasurementsPerRotation >= (360.0f / HorizontalAngleLimit))
                {
                    Reset();
                }
            }

            UpdateMarker.Begin();

            updated = false;
            while (Jobs.Count > 0 && Jobs[0].IsCompleted)
            {
                updated = true;
                Jobs.RemoveAt(0);
            }

            bool jobsIssued = false;
            while (Active.Count > 0)
            {
                var req = Active[0];
                if (!req.RenderTexture.IsCreated())
                {
                    // lost render texture, probably due to Unity window resize or smth
                    req.Readback.WaitForCompletion();
                    req.RenderTexture.Release();
                }
                else if (req.Readback.done)
                {
                    if (req.Readback.hasError)
                    {
                        Debug.Log("Failed to read GPU texture");
                        req.RenderTexture.Release();
                        IgnoreNewRquests = 1.0f;
                    }
                    else
                    {
                        jobsIssued = true;
                        var job = EndReadRequest(req, req.Readback.GetData<byte>());
                        Jobs.Add(job);
                        AvailableRenderTextures.Push(req.RenderTexture);

                        if (req.Index + req.Count >= CurrentMeasurementsPerRotation)
                        {
                            SendMessage();
                        }
                    }
                }
                else
                {
                    break;
                }

                Active.RemoveAt(0);
            }

            if (jobsIssued)
            {
                JobHandle.ScheduleBatchedJobs();
            }

            if (IgnoreNewRquests > 0)
            {
                IgnoreNewRquests -= Time.unscaledDeltaTime;
            }
            else
            {
                float minAngle = 360.0f / CurrentMeasurementsPerRotation;

                AngleDelta += Time.deltaTime * 360.0f * RotationFrequency;
                int count = (int)(HorizontalAngleLimit / minAngle);

                while (AngleDelta >= HorizontalAngleLimit)
                {
                    float angle = AngleStart + HorizontalAngleLimit / 2.0f;
                    var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    Camera.transform.localRotation = rotation;
                    if (Top != null)
                    {
                        Top.transform.localRotation = rotation;
                    }

                    var req = new ReadRequest();
                    if (BeginReadRequest(count, ref req))
                    {
                        req.Readback = AsyncGPUReadback.Request(req.RenderTexture, 0);
                        Active.Add(req);
                    }

                    AngleDelta -= HorizontalAngleLimit;
                    AngleStart += HorizontalAngleLimit;

                    if (AngleStart >= 360.0f)
                    {
                        AngleStart -= 360.0f;
                    }
                }
            }

            UpdateMarker.End();
        }

        public void OnDestroy()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.RenderTexture.Release();
            });

            Jobs.ForEach(job => job.Complete());

            foreach (var tex in AvailableRenderTextures)
            {
                tex.Release();
            }
            foreach (var tex in AvailableTextures)
            {
                DestroyImmediate(tex);
            }

            PointCloudBuffer.Release();

            if (Points.IsCreated)
            {
                Points.Dispose();
            }

            if (PointCloudMaterial != null)
            {
                DestroyImmediate(PointCloudMaterial);
            }

            if (SinLatitudeAngles.IsCreated)
            {
                SinLatitudeAngles.Dispose();
            }
            if (CosLatitudeAngles.IsCreated)
            {
                CosLatitudeAngles.Dispose();
            }
        }

        bool BeginReadRequest(int count, ref ReadRequest req)
        {
            if (count == 0)
            {
                return false;
            }

            BeginReadMarker.Begin();

            RenderTexture texture = null;
            if (AvailableRenderTextures.Count != 0)
            {
                texture = AvailableRenderTextures.Pop();
                if (!texture.IsCreated())
                {
                    texture.Release();
                    texture = null;
                }
            }

            if (texture == null)
            {
                texture = new RenderTexture(RenderTextureWidth, RenderTextureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            texture.Create();

            Camera.targetTexture = texture;
            Camera.Render();

            req = new ReadRequest()
            {
                RenderTexture = texture,
                Index = CurrentIndex,
                Count = count,
                Origin = Camera.transform.position,

                CameraToWorldMatrix = Camera.cameraToWorldMatrix,
            };

            if (!Compensated)
            {
                req.Transform = transform.worldToLocalMatrix;
            }

            BeginReadMarker.End();

            CurrentIndex = (CurrentIndex + count) % CurrentMeasurementsPerRotation;

            return true;
        }

        struct UpdatePointCloudJob : IJob
        {
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<byte> Input;

            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector4> Output;

            // Index of frames
            public int Index;
            // Number of measurements (vertical line) per view frustum
            public int Count;
            // Origin of the camera coordinate system
            public Vector3 Origin;

            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<float> SinLatitudeAngles;
            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<float> CosLatitudeAngles;

            public float SinStartLongitudeAngle;
            public float CosStartLongitudeAngle;
            public float SinDeltaLongitudeAngle;
            public float CosDeltaLongitudeAngle;
            // Scales between world coordinates and texture coordinates
            public float XScale;
            public float YScale;

            public Matrix4x4 Transform;
            public Matrix4x4 CameraToWorldMatrix;

            // Number of laser rays in one measurement (vertical line)
            public int LaserCount;
            // Number of measurements in a whole rotation round
            public int MeasurementsPerRotation;
            // Size of render texture of the camera
            public int TextureWidth;
            public int TextureHeight;
            // Near plane of view frustum
            public float MinDistance;
            // Far plane of view frustum
            public float MaxDistance;

            public bool Compensated;


            public static float DecodeFloatRGB(byte r, byte g, byte b)
            {
                return (r / 255.0f) + (g / 255.0f) / 255.0f + (b / 255.0f) / 65025.0f;
            }

            public void Execute()
            {
                // In the following loop, x/y are in texture space, and xx/yy are in world space
                for (int j = 0; j < LaserCount; j++)
                {
                    float sinLatitudeAngle = SinLatitudeAngles[j];
                    float cosLatitudeAngle = CosLatitudeAngles[j];

                    int indexOffset = j * MeasurementsPerRotation;
                    float dy = cosLatitudeAngle;
                    float rProjected = sinLatitudeAngle;

                    float sinLongitudeAngle = SinStartLongitudeAngle;
                    float cosLongitudeAngle = CosStartLongitudeAngle;

                    for (int i = 0; i < Count; i++)
                    {
                        float dz = rProjected * sinLongitudeAngle;
                        float dx = rProjected * cosLongitudeAngle;

                        float scale = MinDistance / dz;
                        float xx = dx * scale;
                        float yy = dy * scale;
                        int x = (int)(xx / XScale + TextureWidth / 2);
                        int y = (int)(yy / YScale + TextureHeight / 2);
                        int yOffset = y * TextureWidth * 4;

                        float distance;
                        if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight)
                        {
                            distance = 0;
                        }
                        else
                        {
                            byte r = Input[yOffset + x * 4 + 0];
                            byte g = Input[yOffset + x * 4 + 1];
                            byte b = Input[yOffset + x * 4 + 2];
                            distance = 2.0f * DecodeFloatRGB(r, g, b);
                        }

                        int index = indexOffset + (Index + i) % MeasurementsPerRotation;
                        if (distance == 0)
                        {
                            Output[index] = Vector4.zero;
                        }
                        else
                        {
                            byte a = Input[yOffset + x * 4 + 3];
                            float intensity = a / 255.0f;

                            // Note that CameraToWorldMatrix follows OpenGL convention, i.e. camera is facing negative Z axis.
                            // So we have "z" component of the direction as "-MinDistance".
                            Vector3 dir = CameraToWorldMatrix.MultiplyPoint3x4(new Vector3(xx, yy, -MinDistance)) - Origin;
                            var position = Origin + dir.normalized * distance * MaxDistance;

                            if (!Compensated)
                            {
                                position = Transform.MultiplyPoint3x4(position);
                            }
                            Output[index] = new Vector4(position.x, position.y, position.z, intensity);
                        }

                        // We will update longitudeAngle as "longitudeAngle -= DeltaLogitudeAngle".
                        // cos/sin of the new longitudeAngle can be calculated using old cos/sin of logitudeAngle
                        // and cos/sin of DeltaLogitudeAngle (which is constant) via "angle addition and subtraction theorems":
                        // sin(a + b) = sin(a) * cos(b) + cos(a) * sin(b)
                        // cos(a + b) = cos(a) * cos(b) - sin(a) * sin(b)
                        float sinNewLongitudeAngle = sinLongitudeAngle * CosDeltaLongitudeAngle - cosLongitudeAngle * SinDeltaLongitudeAngle;
                        float cosNewLongitudeAngle = cosLongitudeAngle * CosDeltaLongitudeAngle + sinLongitudeAngle * SinDeltaLongitudeAngle;
                        sinLongitudeAngle = sinNewLongitudeAngle;
                        cosLongitudeAngle = cosNewLongitudeAngle;
                    }
                }
            }
        }

        JobHandle EndReadRequest(ReadRequest req, NativeArray<byte> textureData)
        {
            EndReadMarker.Begin();

            var updateJob = new UpdatePointCloudJob()
            {
                Input = new NativeArray<byte>(textureData, Allocator.TempJob),
                Output = Points,

                Index = req.Index,
                Count = req.Count,
                Origin = req.Origin,

                Transform = req.Transform,
                CameraToWorldMatrix = req.CameraToWorldMatrix,

                SinLatitudeAngles = SinLatitudeAngles,
                CosLatitudeAngles = CosLatitudeAngles,

                SinStartLongitudeAngle = SinStartLongitudeAngle,
                CosStartLongitudeAngle = CosStartLongitudeAngle,
                SinDeltaLongitudeAngle = SinDeltaLongitudeAngle,
                CosDeltaLongitudeAngle = CosDeltaLongitudeAngle,
                XScale = XScale,
                YScale = YScale,

                LaserCount = CurrentLaserCount,
                MeasurementsPerRotation = CurrentMeasurementsPerRotation,
                TextureWidth = RenderTextureWidth,
                TextureHeight = RenderTextureHeight,

                MinDistance = MinDistance,
                MaxDistance = MaxDistance,

                Compensated = Compensated,
            };

            EndReadMarker.End();

            return updateJob.Schedule();
        }

        void SendMessage()
        {
            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                var worldToLocal = LidarTransform;
                if (Compensated)
                {
                    worldToLocal = worldToLocal * transform.worldToLocalMatrix;
                }

                Task.Run(() =>
                {
                    Writer.Write(new PointCloudData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Time = SimulatorManager.Instance.CurrentTime,
                        Sequence = SendSequence++,

                        LaserCount = CurrentLaserCount,
                        Transform = worldToLocal,
                        Points = Points,
                    });
                });
            }
        }

        void OnValidate()
        {
            if (TemplateIndex != 0)
            {
                var values = Template.Templates[TemplateIndex];
                if (LaserCount != values.LaserCount ||
                    MinDistance != values.MinDistance ||
                    MaxDistance != values.MaxDistance ||
                    RotationFrequency != values.RotationFrequency ||
                    MeasurementsPerRotation != values.MeasurementsPerRotation ||
                    FieldOfView != values.FieldOfView ||
                    CenterAngle != values.CenterAngle ||
                    !Enumerable.SequenceEqual(VerticalRayAngles, values.VerticalRayAngles))
                {
                    TemplateIndex = 0;
                }
            }
        }

        public NativeArray<Vector4> Capture()
        {
            Debug.Assert(Compensated); // points should be in world-space
            int rotationCount = Mathf.CeilToInt(360.0f / HorizontalAngleLimit);

            float minAngle = 360.0f / CurrentMeasurementsPerRotation;
            int count = (int)(HorizontalAngleLimit / minAngle);

            float angle = HorizontalAngleLimit / 2.0f;

            var jobs = new NativeArray<JobHandle>(rotationCount, Allocator.Persistent);
#if ASYNC
            var active = new ReadRequest[rotationCount];

            try
            {
                for (int i = 0; i < rotationCount; i++)
                {
                    var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    Camera.transform.localRotation = rotation;

                    if (BeginReadRequest(count, angle, HorizontalAngleLimit, ref active[i]))
                    {
                        active[i].Readback = AsyncGPUReadback.Request(active[i].RenderTexture, 0);
                    }

                    angle += HorizontalAngleLimit;
                    if (angle >= 360.0f)
                    {
                        angle -= 360.0f;
                    }
                }

                for (int i = 0; i < rotationCount; i++)
                {
                    active[i].Readback.WaitForCompletion();
                    jobs[i] = EndReadRequest(active[i], active[i].Readback.GetData<byte>());
                }

                JobHandle.CompleteAll(jobs);
            }
            finally
            {
                Array.ForEach(active, req => AvailableRenderTextures.Push(req.RenderTexture));
                jobs.Dispose();
            }
#else
            var textures = new Texture2D[rotationCount];

            var rt = RenderTexture.active;
            try
            {
                for (int i = 0; i < rotationCount; i++)
                {
                    var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    Camera.transform.localRotation = rotation;

                    var req = new ReadRequest();
                    if (BeginReadRequest(count, ref req))
                    {
                        RenderTexture.active = req.RenderTexture;
                        Texture2D texture;
                        if (AvailableTextures.Count > 0)
                        {
                            texture = AvailableTextures.Pop();
                        }
                        else
                        {
                            texture = new Texture2D(RenderTextureWidth, RenderTextureHeight, TextureFormat.RGBA32, false, true);
                        }
                        texture.ReadPixels(new Rect(0, 0, RenderTextureWidth, RenderTextureHeight), 0, 0);
                        textures[i] = texture;
                        jobs[i] = EndReadRequest(req, texture.GetRawTextureData<byte>());

                        AvailableRenderTextures.Push(req.RenderTexture);
                    }

                    angle += HorizontalAngleLimit;
                    if (angle >= 360.0f)
                    {
                        angle -= 360.0f;
                    }
                }

                JobHandle.CompleteAll(jobs);
            }
            finally
            {
                RenderTexture.active = rt;
                Array.ForEach(textures, AvailableTextures.Push);
                jobs.Dispose();
            }
#endif

            return Points;
        }

        public bool Save(string path)
        {
            int rotationCount = Mathf.CeilToInt(360.0f / HorizontalAngleLimit);

            float minAngle = 360.0f / CurrentMeasurementsPerRotation;
            int count = (int)(HorizontalAngleLimit / minAngle);

            float angle = HorizontalAngleLimit / 2.0f;

            var jobs = new NativeArray<JobHandle>(rotationCount, Allocator.Persistent);

            var active = new ReadRequest[rotationCount];

            try
            {
                for (int i = 0; i < rotationCount; i++)
                {
                    var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    Camera.transform.localRotation = rotation;

                    if (BeginReadRequest(count, ref active[i]))
                    {
                        active[i].Readback = AsyncGPUReadback.Request(active[i].RenderTexture, 0);
                    }

                    angle += HorizontalAngleLimit;
                    if (angle >= 360.0f)
                    {
                        angle -= 360.0f;
                    }
                }

                for (int i = 0; i < rotationCount; i++)
                {
                    active[i].Readback.WaitForCompletion();
                    jobs[i] = EndReadRequest(active[i], active[i].Readback.GetData<byte>());
                }

                JobHandle.CompleteAll(jobs);
            }
            finally
            {
                Array.ForEach(active, req => AvailableRenderTextures.Push(req.RenderTexture));
                jobs.Dispose();
            }

            var worldToLocal = LidarTransform;
            if (Compensated)
            {
                worldToLocal = worldToLocal * transform.worldToLocalMatrix;
            }

            try
            {
                using (var writer = new PcdWriter(path))
                {
                    for (int p = 0; p < Points.Length; p++)
                    {
                        var point = Points[p];
                        if (point != Vector4.zero)
                        {
                            writer.Write(worldToLocal.MultiplyPoint3x4(point), point.w);
                        }
                    };
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            VisualizeMarker.Begin();
            if (updated)
            {
                PointCloudBuffer.SetData(Points);
            }

            var lidarToWorld = Compensated ? Matrix4x4.identity : transform.localToWorldMatrix;
            PointCloudMaterial.SetMatrix("_LocalToWorld", lidarToWorld);
            PointCloudMaterial.SetFloat("_Size", PointSize * Utility.GetDpiScale());
            PointCloudMaterial.SetColor("_Color", PointColor);
            Graphics.DrawProcedural(PointCloudMaterial, new Bounds(transform.position, MaxDistance * Vector3.one), MeshTopology.Points, PointCloudBuffer.count, layer: LayerMask.NameToLayer("Sensor"));

            VisualizeMarker.End();
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        public override bool CheckVisible(Bounds bounds)
        {
            var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(Camera);
            return GeometryUtility.TestPlanesAABB(activeCameraPlanes, bounds);
        }
    }
}
