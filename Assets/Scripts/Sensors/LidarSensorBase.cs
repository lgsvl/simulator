/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Simulator.Bridge;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using Simulator.PointCloud;
using PointCloudData = Simulator.Bridge.Data.PointCloudData;

namespace Simulator.Sensors
{
    public abstract class LidarSensorBase : SensorBase
    {
        // Lidar x is forward, y is left, z is up
        public static readonly Matrix4x4 LidarTransform = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero);

        [HideInInspector]
        public int TemplateIndex;

        protected int CurrentLaserCount;
        protected int CurrentMeasurementsPerRotation;
        protected float CurrentFieldOfView;
        protected List<float> CurrentVerticalRayAngles;
        protected float CurrentCenterAngle;
        protected float CurrentMinDistance;
        protected float CurrentMaxDistance;

        // Horizontal FOV of the camera
        protected const float HorizontalAngleLimit = 15.0f;

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

        protected BridgeInstance Bridge;
        protected Publisher<PointCloudData> Publish;
        protected uint SendSequence;

        [NativeDisableContainerSafetyRestriction]
        protected NativeArray<Vector4> Points;

        protected ComputeBuffer PointCloudBuffer;
        int PointCloudLayer;

        protected Material PointCloudMaterial;

        private bool updated;
        protected NativeArray<float> SinLatitudeAngles;
        protected NativeArray<float> CosLatitudeAngles;
        private Camera sensorCamera;

        protected Camera SensorCamera
        {
            get
            {
                if (sensorCamera == null)
                    sensorCamera = GetComponentInChildren<Camera>();

                return sensorCamera;
            }
        }

        protected struct ReadRequest
        {
            public SensorRenderTarget TextureSet;
            public AsyncGPUReadbackRequest Readback;
            public int Index;
            public int Count;
            public float AngleStart;
            public uint TimeStamp;
            public Vector3 Origin;

            public Matrix4x4 Transform;
            public Matrix4x4 CameraToWorldMatrix;
        }

        protected List<ReadRequest> Active = new List<ReadRequest>();
        protected List<JobHandle> Jobs = new List<JobHandle>();

        protected Stack<SensorRenderTarget> AvailableRenderTextures = new Stack<SensorRenderTarget>();
        protected Stack<Texture2D> AvailableTextures = new Stack<Texture2D>();

        int CurrentIndex;
        protected float AngleStart;
        float AngleDelta;

        protected float MaxAngle;
        protected int RenderTextureWidth;
        protected int RenderTextureHeight;
        protected float StartLatitudeAngle;
        protected float EndLatitudeAngle;
        protected float SinStartLongitudeAngle;
        protected float CosStartLongitudeAngle;
        protected float SinDeltaLongitudeAngle;
        protected float CosDeltaLongitudeAngle;

        // Scales between world coordinates and texture coordinates
        protected float XScale;
        protected float YScale;

        protected float IgnoreNewRquests;

        ProfilerMarker UpdateMarker = new ProfilerMarker("Lidar.Update");
        ProfilerMarker VisualizeMarker = new ProfilerMarker("Lidar.Visualzie");
        ProfilerMarker BeginReadMarker = new ProfilerMarker("Lidar.BeginRead");
        protected ProfilerMarker EndReadMarker = new ProfilerMarker("Lidar.EndRead");

        private SensorRenderTarget activeTarget;
        private ShaderTagId passId;

        public override SensorDistributionType DistributionType => SensorDistributionType.UltraHighLoad;

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = bridge.AddPublisher<PointCloudData>(Topic);
        }

        public void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var cmd = CommandBufferPool.Get();
            SensorPassRenderer.Render(context, cmd, hd, activeTarget, passId, Color.clear);
            PointCloudManager.RenderLidar(context, cmd, hd, activeTarget.ColorHandle, activeTarget.DepthHandle);
            CommandBufferPool.Release(cmd);
        }

        public virtual void Init()
        {
            var hd = SensorCamera.GetComponent<HDAdditionalCameraData>();
            hd.hasPersistentHistory = true;
            hd.customRender += CustomRender;
            PointCloudMaterial = new Material(RuntimeSettings.Instance.PointCloudShader);
            PointCloudLayer = LayerMask.NameToLayer("Sensor Effects");
            passId = new ShaderTagId("SimulatorLidarPass");

            Reset();
        }

        private void Start()
        {
            Init();
        }

        protected float CalculateFovAngle(float latitudeAngle, float logitudeAngle)
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

        public abstract void Reset();

        void OnDisable()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.TextureSet.Release();
            });
            Active.Clear();

            Jobs.ForEach(job => job.Complete());
            Jobs.Clear();
        }

        public virtual void Update()
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
            foreach (var req in Active)
            {
                if (!req.TextureSet.IsValid(RenderTextureWidth, RenderTextureHeight))
                {
                    // lost render texture, probably due to Unity window resize or smth
                    req.Readback.WaitForCompletion();
                    req.TextureSet.Release();
                }
                else if (req.Readback.done)
                {
                    if (req.Readback.hasError)
                    {
                        Debug.Log("Failed to read GPU texture");
                        req.TextureSet.Release();
                        IgnoreNewRquests = 1.0f;
                    }
                    else
                    {
                        jobsIssued = true;
                        var job = EndReadRequest(req, req.Readback.GetData<byte>());
                        Jobs.Add(job);
                        AvailableRenderTextures.Push(req.TextureSet);

                        if (req.Index + req.Count >= CurrentMeasurementsPerRotation)
                        {
                            SendMessage();
                        }
                    }
                }
            }
            Active.RemoveAll(req => req.Readback.done == true);

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
                int count = Mathf.CeilToInt(HorizontalAngleLimit / minAngle);

                while (AngleDelta >= HorizontalAngleLimit)
                {
                    float angle = AngleStart + HorizontalAngleLimit / 2.0f;
                    var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                    SensorCamera.transform.localRotation = rotation;
                    if (Top != null)
                    {
                        Top.transform.localRotation = rotation;
                    }

                    var req = new ReadRequest();
                    if (BeginReadRequest(count, ref req))
                    {
                        req.Readback = AsyncGPUReadback.Request((RenderTexture)req.TextureSet.ColorTexture, 0);
                        req.AngleStart = AngleStart;

                        DateTime dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(SimulatorManager.Instance.CurrentTime * 1000.0)).UtcDateTime;
                        DateTime startHour = new DateTime(dt.Year, dt.Month,
                            dt.Day, dt.Hour, 0, 0);
                        TimeSpan timeOverHour = dt - startHour;
                        req.TimeStamp = (uint)(timeOverHour.Ticks / (TimeSpan.TicksPerMillisecond) * 1000);

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

        public virtual void OnDestroy()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.TextureSet.Release();
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

            PointCloudBuffer?.Release();

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

            SensorRenderTarget renderTarget = null;
            if (AvailableRenderTextures.Count != 0)
                renderTarget = AvailableRenderTextures.Pop();

            if (renderTarget == null)
            {
                renderTarget = SensorRenderTarget.Create2D(RenderTextureWidth, RenderTextureHeight);
            }
            else if (!renderTarget.IsValid(RenderTextureWidth, RenderTextureHeight))
            {
                renderTarget.Release();
                renderTarget = SensorRenderTarget.Create2D(RenderTextureWidth, RenderTextureHeight);
            }

            activeTarget = renderTarget;
            SensorCamera.targetTexture = renderTarget;
            SensorCamera.Render();

            req = new ReadRequest()
            {
                TextureSet = renderTarget,
                Index = CurrentIndex,
                Count = count,
                Origin = SensorCamera.transform.position,

                CameraToWorldMatrix = SensorCamera.cameraToWorldMatrix,
            };

            if (!Compensated)
            {
                req.Transform = transform.worldToLocalMatrix;
            }

            BeginReadMarker.End();

            CurrentIndex = (CurrentIndex + count) % CurrentMeasurementsPerRotation;

            return true;
        }

        protected abstract JobHandle EndReadRequest(ReadRequest req, NativeArray<byte> textureData);

        protected abstract void SendMessage();

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
                    SensorCamera.transform.localRotation = rotation;

                    var req = new ReadRequest();
                    if (BeginReadRequest(count, ref req))
                    {
                        RenderTexture.active = req.TextureSet.ColorTexture;
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

                        AvailableRenderTextures.Push(req.TextureSet);
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
                    SensorCamera.transform.localRotation = rotation;

                    if (BeginReadRequest(count, ref active[i]))
                    {
                        active[i].Readback = AsyncGPUReadback.Request(active[i].TextureSet.ColorTexture, 0);
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
                Array.ForEach(active, req => AvailableRenderTextures.Push(req.TextureSet));
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
            var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(SensorCamera);
            return GeometryUtility.TestPlanesAABB(activeCameraPlanes, bounds);
        }
    }
}
