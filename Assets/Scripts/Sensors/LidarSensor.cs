/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

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

namespace Simulator.Sensors
{
    public partial class LidarSensor : SensorBase
    {
        [HideInInspector]
        public int TemplateIndex;

        int CurrentLaserCount;
        int CurrentMeasurementsPerRotation;
        float CurrentFieldOfView;
        float CurrentCenterAngle;
        float CurrentMinDistance;
        float CurrentMaxDistance;

        const float HorizontalAngleLimit = 15.0f;

        [Range(1, 128)]
        public int LaserCount = 32;

        public float MinDistance = 0.5f; // meters
        public float MaxDistance = 100.0f; // meters

        [Range(1, 30)]
        public float RotationFrequency = 5.0f; // Hz

        [Range(18, 6000)] // minmimum is 360/HorizontalAngleLimit
        public int MeasurementsPerRotation = 1500; // for each ray

        [Range(1.0f, 45.0f)]
        public float FieldOfView = 40.0f;

        [Range(-45.0f, 45.0f)]
        public float CenterAngle = 10.0f;

        public bool Compensated = true;
        public bool Visualize = false;

        public Camera Camera = null;
        public GameObject Top = null;

        [Range(1, 10)]
        public float PointSize = 2.0f;
        public Color PointColor = Color.red;

        IBridge Bridge;
        IWriter<PointCloudData> Writer;
        uint SendSequence;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Vector4> Points;

        ComputeBuffer PointCloudBuffer;
        int PointCloudLayer;

        Material PointCloudMaterial;

        struct ReadRequest
        {
            public RenderTexture Texture;
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
        Stack<RenderTexture> Available = new Stack<RenderTexture>();
        List<JobHandle> Jobs = new List<JobHandle>();

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
                var drawing = new DrawingSettings(new ShaderTagId("LidarPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }

        public void Start()
        {
            PointCloudMaterial = new Material(RuntimeSettings.Instance.PointCloudShader);
            PointCloudLayer = LayerMask.NameToLayer("Sensor Effects");

            Camera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;

            Reset();
        }

        public void Reset()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.Texture.Release();
            });
            Active.Clear();

            Jobs.ForEach(job => job.Complete());
            Jobs.Clear();

            foreach (var req in Available)
            {
                req.Release();
            };
            Available.Clear();

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
            Camera.projectionMatrix = projection;

            int count = LaserCount * MeasurementsPerRotation;
            PointCloudBuffer = new ComputeBuffer(count, UnsafeUtility.SizeOf<Vector4>());
            PointCloudMaterial.SetBuffer("_PointCloud", PointCloudBuffer);

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
                req.Texture.Release();
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

            bool updated = false;
            while (Jobs.Count > 0 && Jobs[0].IsCompleted)
            {
                updated = true;
                Jobs.RemoveAt(0);
            }

            bool jobsIssued = false;
            while (Active.Count > 0)
            {
                var req = Active[0];
                if (!req.Texture.IsCreated())
                {
                    // lost render texture, probably due to Unity window resize or smth
                    req.Readback.WaitForCompletion();
                    req.Texture.Release();
                }
                else if (req.Readback.done)
                {
                    jobsIssued = true;
                    EndReadRequest(req);
                    Available.Push(req.Texture);
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

                BeginReadRequest(count, AngleStart, HorizontalAngleLimit);
                jobsIssued = true;

                AngleDelta -= HorizontalAngleLimit;
                AngleStart += HorizontalAngleLimit;

                if (AngleStart >= 360.0f)
                {
                    AngleStart -= 360.0f;
                }
            }

            if (Visualize)
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
                Graphics.DrawProcedural(PointCloudMaterial, new Bounds(Vector3.zero, 1000 * Vector3.one), MeshTopology.Points, PointCloudBuffer.count, layer: 1);

                VisualizeMarker.End();
            }

            UpdateMarker.End();
        }

        void OnDestroy()
        {
            Active.ForEach(req =>
            {
                req.Readback.WaitForCompletion();
                req.Texture.Release();
            });

            Jobs.ForEach(job => job.Complete());

            foreach (var req in Available)
            {
                req.Release();
            }

            PointCloudBuffer.Release();

            if (Points.IsCreated)
            {
                Points.Dispose();
            }

            Destroy(PointCloudMaterial);
        }

        void BeginReadRequest(int count, float angleStart, float angleUse)
        {
            if (count == 0)
            {
                return;
            }

            BeginReadMarker.Begin();

            RenderTexture texture;
            if (Available.Count == 0)
            {
                texture = new RenderTexture(RenderTextureWidth, RenderTextureHeight, 24, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            }
            else
            {
                texture = Available.Pop();
            }

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

            var request = new ReadRequest()
            {
                Readback = AsyncGPUReadback.Request(texture, 0),
                Texture = texture,
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
                request.Transform = transform.worldToLocalMatrix;
            }

            Active.Add(request);
            CurrentIndex = (CurrentIndex + count) % CurrentMeasurementsPerRotation;

            BeginReadMarker.End();
        }

        struct UpdatePointCloudJob : IJob
        {
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<Vector2> Input;

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

            public bool Compensated;

            public void Execute()
            {
                var startDir = Start + StartRay * DeltaY;

                for (int j = 0; j < LaserCount; j++)
                {
                    var dir = startDir;
                    int y = (j + StartRay) * TextureHeight / MaxRayCount;
                    int yOffset = y * TextureWidth;
                    int indexOffset = j * MeasurementsPerRotation;

                    for (int i = 0; i < Count; i++)
                    {
                        int x = i * TextureWidth / Count;

                        var di = Input[yOffset + x];
                        float distance = di.x;
                        float intensity = di.y;

                        var position = Origin + dir.normalized * distance;

                        int index = indexOffset + (Index + i) % MeasurementsPerRotation;
                        if (!Compensated)
                        {
                            position = Transform.MultiplyPoint3x4(position);
                        }
                        Output[index] = distance == 0 ? Vector4.zero : new Vector4(position.x, position.y, position.z, intensity);

                        dir += DeltaX;
                    }

                    startDir += DeltaY;
                }
            }
        }

        void EndReadRequest(ReadRequest req)
        {
            EndReadMarker.Begin();

            var updateJob = new UpdatePointCloudJob()
            {
                Input = new NativeArray<Vector2>(req.Readback.GetData<Vector2>(), Allocator.TempJob),
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

                Compensated = Compensated,
            };

            Jobs.Add(updateJob.Schedule());

            if (req.Index + req.Count >= CurrentMeasurementsPerRotation)
            {
                if (Bridge != null && Bridge.Status == Status.Connected)
                {
                    // Lidar x is forward, y is left, z is up
                    var worldToLocal = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero);
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
                            Sequence = SendSequence++,
                            LaserCount = CurrentLaserCount,
                            Transform = worldToLocal,
                            Points = Points,
                        });
                    });
                }
            }

            EndReadMarker.End();
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
    }
}
