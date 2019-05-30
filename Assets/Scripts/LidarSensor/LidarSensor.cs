/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

public class LidarSensor : MonoBehaviour, Comm.BridgeClient
{
    [HideInInspector]
    public int Template;

    int CurrentRayCount;
    int CurrentMeasurementsPerRotation;
    float CurrentFieldOfView;
    float CurrentCenterAngle;
    float CurrentMinDistance;
    float CurrentMaxDistance;

    const float HorizontalAngleLimit = 15.0f;

    [Range(1, 128)]
    public int RayCount = 32;

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

    public Shader Shader = null;
    public Camera Camera = null;
    public GameObject Top = null;

    public Material PointCloudMaterial = null;
    public Color PointColor = Color.red;

    public ROSTargetEnvironment TargetEnvironment;
    public string TopicName = "/simulator/sensors/lidar";
    public string FrameName = "velodyne";
    public string AutowareTopicName = "/points_raw";
    public string ApolloTopicName = "/apollo/sensor/velodyne64/compensator/PointCloud2";

    private GameObject Agent = null;
    private AgentSetup agentSetup = null;
    private bool isVisualize = true;

    public bool Compensated = true;

    Comm.Bridge Bridge;
    Comm.Writer<Ros.PointCloud2> ApolloWriterPointCloud2;
    Comm.Writer<apollo.drivers.PointCloud> Apollo35WriterPointCloud2;
    Comm.Writer<Ros.PointCloud2> AutowareWriterPointCloud2;
    Comm.Writer<Ros.LaserScan> WriterLaserScan;
    Comm.Writer<Ros.PointCloud2> WriterPointCloud2;
    uint Sequence;

    Vector4[] PointCloud;
    byte[] RosPointCloud;

    ComputeBuffer PointCloudBuffer;
    int PointCloudLayerMask;

    struct ReadRequest
    {
        public AsyncTextureReader<Vector2> Reader;
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
    Stack<AsyncTextureReader<Vector2>> Available = new Stack<AsyncTextureReader<Vector2>>();

    int CurrentIndex;
    float AngleStart;
    float AngleDelta;

    float AngleTopPart;

    float DebugStartAngle;
    int DebugCount;

    float MaxAngle;
    int RenderTextureWidth;
    int RenderTextureHeight;

    float FixupAngle;

    public void ApplyTemplate()
    {
        var values = LidarTemplate.Templates[Template];
        RayCount = values.RayCount;
        MinDistance = values.MinDistance;
        MaxDistance = values.MaxDistance;
        RotationFrequency = values.RotationFrequency;
        MeasurementsPerRotation = values.MeasurementsPerRotation;
        FieldOfView = values.FieldOfView;
        CenterAngle = values.CenterAngle;
    }

    void Awake()
    {
        PointCloudMaterial = Object.Instantiate(PointCloudMaterial);

        if (Agent == null)
            Agent = transform.root.gameObject;
        agentSetup = Agent?.GetComponent<AgentSetup>();
        var lidarCheckbox = Agent.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleLidar", "Enable LIDAR:", false);
        lidarCheckbox.onValueChanged.AddListener(x => enabled = x);

        PointCloudLayerMask = 1 << LayerMask.NameToLayer("Sensor Effects");

        Camera.enabled = false;
        Camera.renderingPath = RenderingPath.Forward;
        Camera.clearFlags = CameraClearFlags.Color;
        Camera.backgroundColor = Color.black;
        Camera.allowMSAA = false;
        Camera.allowHDR = false;

        Reset();
    }

    public void Reset()
    {
        Active.ForEach(req =>
        {
            req.Reader.Destroy();
            req.Reader.Texture.Release();
        });
        Active.Clear();

        foreach (var rt in Available)
        {
            rt.Destroy();
            rt.Texture.Release();
        };
        Available.Clear();

        if (PointCloudBuffer != null)
        {
            PointCloudBuffer.Release();
            PointCloudBuffer = null;
        }

        FixupAngle = AngleStart = 0.0f;
        MaxAngle = Mathf.Abs(CenterAngle) + FieldOfView / 2.0f;
        RenderTextureHeight = 2 * (int)(2.0f * MaxAngle * RayCount / FieldOfView);
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

        int count = RayCount * MeasurementsPerRotation;
        PointCloud = new Vector4[count];
        PointCloudBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector4)));

        RosPointCloud = new byte[32 * count];

        CurrentRayCount = RayCount;
        CurrentMeasurementsPerRotation = MeasurementsPerRotation;
        CurrentFieldOfView = FieldOfView;
        CurrentCenterAngle = CenterAngle;
        CurrentMinDistance = MinDistance;
        CurrentMaxDistance = MaxDistance;
    }

    private void OnEnable()
    {
        Reset();
    }

    void OnDisable()
    {
        Active.ForEach(req =>
        {
            req.Reader.Destroy();
            req.Reader.Texture.Release();
        });
        Active.Clear();
    }

    void Update()
    {
        if (Agent == null)
        {
            Debug.Log("Agent or camera is null!");
            return;
        }

        if (RayCount != CurrentRayCount ||
            MeasurementsPerRotation != CurrentMeasurementsPerRotation ||
            FieldOfView != CurrentFieldOfView ||
            CenterAngle != CurrentCenterAngle || 
            MinDistance != CurrentMinDistance ||
            MaxDistance != CurrentMaxDistance)
        {
            if (MinDistance > 0 && MaxDistance > 0 && RayCount > 0 && MeasurementsPerRotation >= (360.0f / HorizontalAngleLimit))
            {
                Reset();
            }
        }

        bool pointCloudUpdated = false;

        for (int i = 0; i < Active.Count; i++)
        {
            var req = Active[i];
            req.Reader.Update();
            if (req.Reader.Status == AsyncTextureReaderStatus.Finished)
            {
                pointCloudUpdated = true;
                ReadLasers(req);
                Available.Push(req.Reader);
                Active.RemoveAt(i);
                i--;
            }
            else if (req.Reader.Status == AsyncTextureReaderStatus.Idle)
            {
                // reader was reset, probably due to loosing RenderTexture
                Available.Push(req.Reader);
                Active.RemoveAt(i);
                i--;
            }
            else
            {
                for (int j=i+1; j<Active.Count; j++)
                {
                    Active[j].Reader.Update();
                }
                break;
            }
        }

        float minAngle = 360.0f / CurrentMeasurementsPerRotation;

        AngleDelta += Time.deltaTime * 360.0f * RotationFrequency;
        int count = (int)(HorizontalAngleLimit / minAngle);

        while (AngleDelta >= HorizontalAngleLimit)
        {
            float angle = AngleStart + HorizontalAngleLimit / 2.0f;
            var rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Camera.transform.localRotation = rotation;
            Top.transform.localRotation = rotation;

            if (count != 0)
            {
                pointCloudUpdated |= RenderLasers(count, AngleStart, HorizontalAngleLimit);
            }

            AngleDelta -= HorizontalAngleLimit;
            AngleStart += HorizontalAngleLimit;

            if (AngleStart >= 360.0f)
            {
                AngleStart -= 360.0f;
            }
        }

        if (agentSetup.isSensorEffect && pointCloudUpdated)
        {
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("Update Point Cloud Buffer");
#endif
            PointCloudBuffer.SetData(PointCloud);
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }
    }

    void OnDestroy()
    {
        PointCloudBuffer.Release();

        Active.ForEach(req =>
        {
            req.Reader.Destroy();
            req.Reader.Texture.Release();
        });
        Active.Clear();

        foreach (var rt in Available)
        {
            rt.Destroy();
            rt.Texture.Release();
        };
        Available.Clear();
    }

    bool RenderLasers(int count, float angleStart, float angleUse)
    {
        bool pointCloudUpdated = false;
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.BeginSample("Render Lasers");
#endif

        AsyncTextureReader<Vector2> reader = null;
        if (Available.Count == 0)
        {
            var texture = new RenderTexture(RenderTextureWidth, RenderTextureHeight, 24, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            reader = new AsyncTextureReader<Vector2>(texture);
        }
        else
        {
            reader = Available.Pop();
        }

        Camera.targetTexture = reader.Texture;
        Camera.RenderWithShader(Shader, "RenderType");
        reader.Start();

        var pos = Camera.transform.position;

        var topLeft = Camera.ViewportPointToRay(new Vector3(0, 0, 1)).direction;
        var topRight = Camera.ViewportPointToRay(new Vector3(1, 0, 1)).direction;
        var bottomLeft = Camera.ViewportPointToRay(new Vector3(0, 1, 1)).direction;
        var bottomRight = Camera.ViewportPointToRay(new Vector3(1, 1, 1)).direction;

        int maxRayCount = (int)(2.0f * MaxAngle * RayCount / FieldOfView);
        var deltaX = (topRight - topLeft) / count;
        var deltaY = (bottomLeft - topLeft) / maxRayCount;

        int startRay = 0;
        var start = topLeft;
        if (CenterAngle < 0.0f)
        {
            startRay = maxRayCount - RayCount;
        }

#if VISUALIZE_LIDAR_CAMERA_BOUNDING_BOX
        var a = start + deltaY * startRay;
        var b = a + deltaX * count;

        Debug.DrawLine(pos, pos + MaxDistance * a, Color.yellow, 1.0f, true);
        Debug.DrawLine(pos, pos + MaxDistance * b, Color.yellow, 1.0f, true);
        Debug.DrawLine(pos + MaxDistance * a, pos + MaxDistance * b, Color.yellow, 1.0f, true);

        a = start + deltaY * (startRay + RayCount);
        b = a + deltaX * count;

        Debug.DrawLine(pos, pos + MaxDistance * a, Color.magenta, 1.0f, true);
        Debug.DrawLine(pos, pos + MaxDistance * b, Color.magenta, 1.0f, true);
        Debug.DrawLine(pos + MaxDistance * a, pos + MaxDistance * b, Color.magenta, 1.0f, true);
#endif

        var req = new ReadRequest() {
            Reader = reader,
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

        req.Reader.Update();
        if (req.Reader.Status == AsyncTextureReaderStatus.Finished)
        {
            pointCloudUpdated = true;
            ReadLasers(req);
            Available.Push(req.Reader);
        }
        else
        {
            Active.Add(req);
        }
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif
        return pointCloudUpdated;
    }

    void ReadLasers(ReadRequest req)
    {
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.BeginSample("Read Lasers");
#endif
        var data = req.Reader.GetData();

        var startRay = req.StartRay;
        var maxRayCount = req.MaxRayCount;

        var startDir = req.Start + startRay * req.DeltaY;
        var origin = req.Origin;

        for (int j = 0; j < CurrentRayCount; j++)
        {
            var dir = startDir;
            int y = (j + startRay) * RenderTextureHeight / maxRayCount;
            int yOffset = y * RenderTextureWidth;
            int indexOffset = j * CurrentMeasurementsPerRotation;

            for (int i = 0; i < req.Count; i++)
            {
                int x = i * RenderTextureWidth / req.Count;

                var di = data[yOffset + x];
                float distance = di.x;
                float intensity = di.y;

                var position = origin + dir.normalized * distance;

                int index = indexOffset + (CurrentIndex + i) % CurrentMeasurementsPerRotation;
                if (!Compensated)
                {
                    position = req.Transform.MultiplyPoint3x4(position);
                }
                PointCloud[index] = distance == 0 ? Vector4.zero : new Vector4(position.x, position.y, position.z, intensity);

                dir += req.DeltaX;
            }

            startDir += req.DeltaY;
        }

        if (CurrentIndex + req.Count >= CurrentMeasurementsPerRotation)
        {
            SendMessage(CurrentIndex + req.Count - CurrentMeasurementsPerRotation, req.AngleEnd);
        }

        CurrentIndex = (CurrentIndex + req.Count) % CurrentMeasurementsPerRotation;

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    void SendMessage(int currentEndIndex, float currentEndAngle)
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.BeginSample("SendMessage");
#endif

        // Lidar x is forward, y is left, z is up
        var  worldToLocal = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero);

        if (Compensated)
        {
            worldToLocal = worldToLocal * transform.worldToLocalMatrix;
        }

        if (CurrentRayCount != 1)
        {
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            double measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;

            if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
            {
                var msg = new apollo.drivers.PointCloud()
                {
                    header = new apollo.common.Header()
                    {
                        // TimestampSec = measurement_time,
                        sequence_num = Sequence++,
                        frame_id = FrameName,
                    },
                    frame_id = "lidar128",
                    is_dense = false,
                    measurement_time = measurement_time,
                    height = 1,
                    width = (uint)PointCloud.Length, // TODO is this right?
                };

                for (int i = 0; i < PointCloud.Length; i++)
                {
                    var point = PointCloud[i];
                    if (point == Vector4.zero)
                    {
                        continue;
                    }

                    var pos = new Vector3(point.x, point.y, point.z);
                    float intensity = point.w;

                    pos = worldToLocal.MultiplyPoint3x4(pos);

                    msg.point.Add(new apollo.drivers.PointXYZIT() {
                            x = pos.x,
                            y = pos.y,
                            z = pos.z,
                            intensity = (byte)(intensity * 255),
                            timestamp = (ulong)measurement_time                   
                        });
                };

                Apollo35WriterPointCloud2.Publish(msg);
            }

            else
            {
                int count = 0;
                unsafe
                {
                    fixed (byte* ptr = RosPointCloud)
                    {
                        int offset = 0;
                        for (int i = 0; i < PointCloud.Length; i++)
                        {
                            var point = PointCloud[i];
                            if (point == Vector4.zero)
                            {
                                continue;
                            }

                            var pos = new Vector3(point.x, point.y, point.z);
                            float intensity = point.w;

                            *(Vector3*)(ptr + offset) = worldToLocal.MultiplyPoint3x4(pos);
                            *(ptr + offset + 16) = (byte)(intensity * 255);

                            offset += 32;
                            count++;
                        }
                    }
                }

                var msg = new Ros.PointCloud2()
                {
                    header = new Ros.Header()
                    {
                        stamp = Ros.Time.Now(),
                        seq = Sequence++,
                        frame_id = FrameName,
                    },
                    height = 1,
                    width = (uint)count,
                    fields = new Ros.PointField[] {
                        new Ros.PointField()
                        {
                            name = "x",
                            offset = 0,
                            datatype = 7,
                            count = 1,
                        },
                        new Ros.PointField()
                        {
                            name = "y",
                            offset = 4,
                            datatype = 7,
                            count = 1,
                        },
                        new Ros.PointField()
                        {
                            name = "z",
                            offset = 8,
                            datatype = 7,
                            count = 1,
                        },
                        new Ros.PointField()
                        {
                            name = "intensity",
                            offset = 16,
                            datatype = 2,
                            count = 1,
                        },
                        new Ros.PointField()
                        {
                            name = "timestamp",
                            offset = 24,
                            datatype = 8,
                            count = 1,
                        },
                    },
                    is_bigendian = false,
                    point_step = 32,
                    row_step = (uint)count * 32,
                    data = new Ros.PartialByteArray()
                    {
                        Array = RosPointCloud,
                        Length = count * 32,
                    },
                    is_dense = true,
                };

                if (TargetEnvironment == ROSTargetEnvironment.APOLLO)
                {
                    ApolloWriterPointCloud2.Publish(msg);
                }          
                
                else if (TargetEnvironment == ROSTargetEnvironment.AUTOWARE)
                {
                    AutowareWriterPointCloud2.Publish(msg);
                }

                else
                {
                    WriterPointCloud2.Publish(msg);
                }
            }

        }
        else // for planar lidar only (single ray lidar) publish LaserScan instead of PoinCloud2
        {

            float fixup = currentEndAngle;
            FixupAngle = currentEndAngle;

            float LaserScanMinAngle = -Mathf.PI;
            float LaserScanMaxAngle = Mathf.PI;
            float LaserScanMinRange = 0.0f;
            float LaserScanMaxRange = 40.0f;

            float[] range_array = new float[CurrentMeasurementsPerRotation];
            float[] intensity_array = new float[CurrentMeasurementsPerRotation];

            int offset = (int)(currentEndIndex - currentEndAngle / 360.0f * CurrentMeasurementsPerRotation);

            for (int i = 0; i < PointCloud.Length; i++)
            {
                var point = PointCloud[i];
                if (point == Vector4.zero)
                {
                    continue;
                }
                range_array[(i + offset + CurrentMeasurementsPerRotation) % CurrentMeasurementsPerRotation] = Vector3.Distance(new Vector3(point.x, point.y, point.z), transform.position);
                intensity_array[(i + offset + CurrentMeasurementsPerRotation) % CurrentMeasurementsPerRotation] = point.w;
            }
            // Populate LaserScan message
            var msg = new Ros.LaserScan()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = Sequence++,
                    frame_id = FrameName,
                },
                angle_min = LaserScanMinAngle,
                angle_max = LaserScanMaxAngle,
                angle_increment = Mathf.Abs(LaserScanMaxAngle - LaserScanMinAngle)/CurrentMeasurementsPerRotation,
                time_increment = 1.0f/(CurrentMeasurementsPerRotation*RotationFrequency),
                scan_time = 1.0f/RotationFrequency,
                range_min = LaserScanMinRange,
                range_max = LaserScanMaxRange,
                ranges = range_array, // TODO: Should make sure only sending range between min and max
                intensities = intensity_array,
            };
            WriterLaserScan.Publish(msg);
        }

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    public bool Save(string path)
    {
        float minAngle = 360.0f / CurrentMeasurementsPerRotation;
        int laserCount = (int)(HorizontalAngleLimit / minAngle);

        float angle = 0f;
        while (angle < 360f)
        {
            var rotation = Quaternion.AngleAxis(angle + HorizontalAngleLimit / 2.0f, Vector3.up);
            Camera.transform.localRotation = rotation;
            Top.transform.localRotation = rotation;

            RenderLasers(laserCount, AngleStart, HorizontalAngleLimit);

            angle += HorizontalAngleLimit;
        }

        for (int i = 0; i < Active.Count; i++)
        {
            var req = Active[i];
            req.Reader.Update(true);
            if (req.Reader.Status == AsyncTextureReaderStatus.Finished)
            {
                ReadLasers(req);
                Available.Push(req.Reader);
                Active.RemoveAt(i);
                i--;
            }
            else if (req.Reader.Status == AsyncTextureReaderStatus.Idle)
            {
                // reader was reset, probably due to loosing RenderTexture
                Available.Push(req.Reader);
                Active.RemoveAt(i);
                i--;
            }
        }

        // Lidar x is forward, y is left, z is up
        var  worldToLocal = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero);

        if (Compensated)
        {
            worldToLocal = worldToLocal * transform.worldToLocalMatrix;
        }

        try
        {
            using (var file = System.IO.File.Create(path, 1024 * 1024))
            {
                System.Action<string> write = (string s) =>
                {
                    var bytes = System.Text.Encoding.ASCII.GetBytes(s);
                    file.Write(bytes, 0, bytes.Length);
                };

                int count = PointCloud.Count(v => v != Vector4.zero);

                write($"VERSION 0.7\n");
                write($"FIELDS x y z intensity\n");
                write($"SIZE 4 4 4 1\n");
                write($"TYPE F F F U\n");
                write($"COUNT 1 1 1 1\n");
                write($"WIDTH {count}\n");
                write($"HEIGHT 1\n");
                write($"VIEWPOINT 0 0 0 1 0 0 0\n");
                write($"POINTS {count}\n");
                write($"DATA binary\n");

                var buffer = new byte[4 + 4 + 4 + 1];
                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        for (int i = 0; i < PointCloud.Length; i++)
                        {
                            var point = PointCloud[i];
                            if (point == Vector4.zero)
                            {
                                continue;
                            }

                            var pos = new Vector3(point.x, point.y, point.z);
                            float intensity = point.w;

                            *(Vector3*)ptr = worldToLocal.MultiplyPoint3x4(pos);
                            ptr[4 + 4 + 4] = (byte)(intensity * 255);

                            file.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
            return true;
        }
        catch
        {
        }

        return false;
    }

    void OnRenderObject()
    {
        if (isVisualize && agentSetup.isSensorEffect && (Camera.current.cullingMask & PointCloudLayerMask) != 0)
        {
            var lidarToWorld = Compensated ? Matrix4x4.identity :  transform.localToWorldMatrix;
            PointCloudMaterial.SetMatrix("_LidarToWorld", lidarToWorld);
            PointCloudMaterial.SetBuffer("PointCloud", PointCloudBuffer);
            PointCloudMaterial.SetColor("_Color", PointColor);
            PointCloudMaterial.SetPass(0);
            Graphics.DrawProcedural(MeshTopology.Points, PointCloud.Length);
        }
    }

    void OnValidate()
    {
        if (Template != 0)
        {
            var values = LidarTemplate.Templates[Template];
            if (RayCount != values.RayCount ||
                MinDistance != values.MinDistance || 
                MaxDistance != values.MaxDistance ||
                RotationFrequency != values.RotationFrequency ||
                MeasurementsPerRotation != values.MeasurementsPerRotation ||
                FieldOfView != values.FieldOfView ||
                CenterAngle != values.CenterAngle)
            {
                Template = 0;
            }
        }
    }

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            if (TargetEnvironment == ROSTargetEnvironment.APOLLO)
            {
                ApolloWriterPointCloud2 = Bridge.AddWriter<Ros.PointCloud2>(ApolloTopicName);
            }
            else if (TargetEnvironment == ROSTargetEnvironment.AUTOWARE)
            {
                AutowareWriterPointCloud2 = Bridge.AddWriter<Ros.PointCloud2>(AutowareTopicName);
            }
            else if (TargetEnvironment == ROSTargetEnvironment.APOLLO35)
            {
                Apollo35WriterPointCloud2 = Bridge.AddWriter<apollo.drivers.PointCloud>(ApolloTopicName);
            }
            else
            {
                if (RayCount == 1)
                {
                    WriterLaserScan = Bridge.AddWriter<Ros.LaserScan>(TopicName);
                }
                else
                {
                    WriterPointCloud2 = Bridge.AddWriter<Ros.PointCloud2>(TopicName);
                }
            }
        };
    }

    public void EnableVisualize(bool enable)
    {
        isVisualize = enable;
    }
}
