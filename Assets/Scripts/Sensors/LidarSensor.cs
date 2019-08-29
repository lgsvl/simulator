/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
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
        float CurrentCenterAngle;
        float CurrentMinDistance;
        float CurrentMaxDistance;

        const float HorizontalAngleLimit = 15.0f;

        [SensorParameter]
        [Range(1, 128)]
        public int LaserCount = 32;

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
        [Range(1.0f, 45.0f)]
        public float FieldOfView = 40.0f;

        [SensorParameter]
        [Range(-45.0f, 45.0f)]
        public float CenterAngle = 10.0f;

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

        struct ReadRequest
        {
            public RenderTexture RenderTexture;
            public AsyncGPUReadbackRequest Readback;
            public int Index;
            public int Count;
            public int MaxRayCount;
            public int StartRay;

            public Vector3 Origin;
            public Vector3 Start;
            public Vector3 DeltaX;
            public Vector3 DeltaY;

            public Matrix4x4 Transform;

            public float AngleEnd;
        }

        List<ReadRequest> Active = new List<ReadRequest>();
        List<JobHandle> Jobs = new List<JobHandle>();

        Stack<RenderTexture> AvailableRenderTextures = new Stack<RenderTexture>();
        Stack<Texture2D> AvailableTextures = new Stack<Texture2D>();

        int CurrentIndex;
        float AngleStart;
        float AngleDelta;

        float AngleTopPart;

        float MaxAngle;
        int RenderTextureWidth;
        int RenderTextureHeight;

        float FixupAngle;

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

        private void Awake()
        {
            Camera = GetComponentInChildren<Camera>();
        }

        public void Init()
        {
            Camera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;
        }

        public void Start()
        {
            PointCloudMaterial = new Material(RuntimeSettings.Instance.PointCloudShader);
            PointCloudLayer = LayerMask.NameToLayer("Sensor Effects");

            Init();
            Reset();
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

            FixupAngle = AngleStart = 0.0f;
            MaxAngle = Mathf.Abs(CenterAngle) + FieldOfView / 2.0f;
            RenderTextureHeight = 2 * (int)(2.0f * MaxAngle * LaserCount / FieldOfView);
            RenderTextureWidth = 2 * (int)(HorizontalAngleLimit / (360.0f / MeasurementsPerRotation));

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

            int count = LaserCount * MeasurementsPerRotation;
            PointCloudBuffer = new ComputeBuffer(count, UnsafeUtility.SizeOf<Vector4>());
            PointCloudMaterial?.SetBuffer("_PointCloud", PointCloudBuffer);

            Points = new NativeArray<Vector4>(count, Allocator.Persistent);

            CurrentLaserCount = LaserCount;
            CurrentMeasurementsPerRotation = MeasurementsPerRotation;
            CurrentFieldOfView = FieldOfView;
            CurrentCenterAngle = CenterAngle;
            CurrentMinDistance = MinDistance;
            CurrentMaxDistance = MaxDistance;
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
                MaxDistance != CurrentMaxDistance)
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
                if (BeginReadRequest(count, AngleStart, HorizontalAngleLimit, ref req))
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
                Destroy(PointCloudMaterial);
            }
        }
        
        bool BeginReadRequest(int count, float angleStart, float angleUse, ref ReadRequest req)
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

            var pos = Camera.transform.position;

            var topLeft = Camera.ViewportPointToRay(new Vector3(0, 0, 1)).direction;
            var topRight = Camera.ViewportPointToRay(new Vector3(1, 0, 1)).direction;
            var bottomLeft = Camera.ViewportPointToRay(new Vector3(0, 1, 1)).direction;
            var bottomRight = Camera.ViewportPointToRay(new Vector3(1, 1, 1)).direction;

            int maxRayCount = (int)(2.0f * MaxAngle * LaserCount / FieldOfView);
            var deltaX = (topRight - topLeft) / count;
            var deltaY = (bottomLeft - topLeft) / maxRayCount;

            int startRay = 0;
            var start = topLeft;
            if (CenterAngle < 0.0f)
            {
                startRay = maxRayCount - LaserCount;
            }

            req = new ReadRequest()
            {
                RenderTexture = texture,
                Index = CurrentIndex,
                Count = count,
                MaxRayCount = maxRayCount,
                StartRay = startRay,
                Origin = pos,
                Start = start,
                DeltaX = deltaX,
                DeltaY = deltaY,
                AngleEnd = angleStart + angleUse,
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

            public int Index;
            public int Count;
            public int MaxRayCount;
            public int StartRay;

            public Vector3 Origin;
            public Vector3 Start;
            public Vector3 DeltaX;
            public Vector3 DeltaY;

            public Matrix4x4 Transform;

            public int LaserCount;
            public int MeasurementsPerRotation;
            public int TextureWidth;
            public int TextureHeight;

            public float MinDistance;
            public float MaxDistance;

            public bool Compensated;

            public static float DecodeFloatRGB(byte r, byte g, byte b)
            {
                return (r / 255.0f) + (g / 255.0f) / 255.0f + (b / 255.0f) / 65025.0f;
            }

            public void Execute()
            {
                var startDir = Start + StartRay * DeltaY;

                for (int j = 0; j < LaserCount; j++)
                {
                    var dir = startDir;
                    int y = (j + StartRay) * TextureHeight / MaxRayCount;
                    int yOffset = y * TextureWidth * 4;
                    int indexOffset = j * MeasurementsPerRotation;

                    for (int i = 0; i < Count; i++)
                    {
                        int x = i * TextureWidth / Count;

                        byte r = Input[yOffset + x * 4 + 0];
                        byte g = Input[yOffset + x * 4 + 1];
                        byte b = Input[yOffset + x * 4 + 2];
                        float distance = 2.0f * DecodeFloatRGB(r, g, b);

                        int index = indexOffset + (Index + i) % MeasurementsPerRotation;
                        if (distance == 0)
                        {
                            Output[index] = Vector4.zero;
                        }
                        else
                        {
                            byte a = Input[yOffset + x * 4 + 3];
                            float intensity = a / 255.0f;

                            var position = Origin + dir.normalized * distance * MaxDistance;

                            if (!Compensated)
                            {
                                position = Transform.MultiplyPoint3x4(position);
                            }
                            Output[index] = new Vector4(position.x, position.y, position.z, intensity);
                        }

                        dir += DeltaX;
                    }

                    startDir += DeltaY;
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
                MaxRayCount = req.MaxRayCount,
                StartRay = req.StartRay,

                Origin = req.Origin,
                Start = req.Start,
                DeltaX = req.DeltaX,
                DeltaY = req.DeltaY,

                Transform = req.Transform,

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
                    CenterAngle != values.CenterAngle)
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
                    if (BeginReadRequest(count, angle, HorizontalAngleLimit, ref req))
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
            PointCloudMaterial.SetFloat("_Size", PointSize);
            PointCloudMaterial.SetColor("_Color", PointColor);
            Graphics.DrawProcedural(PointCloudMaterial, new Bounds(transform.position, MaxDistance * Vector3.one), MeshTopology.Points, PointCloudBuffer.count, layer: LayerMask.NameToLayer("Sensor"));

            VisualizeMarker.End();
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
