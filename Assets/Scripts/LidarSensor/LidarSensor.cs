/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class LidarSensor : MonoBehaviour, Ros.IRosClient
{
    [HideInInspector]
    public int Template;

    int CurrentRayCount;
    int CurrentMeasurementsPerRotation;

    const float HorizontalRenderHalfAngleLimit = 15.0f;

    [Range(1, 128)]
    public int RayCount = 32;

    public float MinDistance = 0.5f; // meters
    public float MaxDistance = 100.0f; // meters

    [Range(1, 30)]
    public float RotationFrequency = 5.0f; // Hz

    [Range(18, 6000)] // minmimum is 360/HorizontalRenderHalfAngleLimit
    public int MeasurementsPerRotation = 3125; // for each ray

    [Range(1.0f, 45.0f)]
    public float FieldOfView = 26.8f;

    [Range(-45.0f, 45.0f)]
    public float CenterAngle = 0.0f;

    public Shader Shader = null;
    public Camera Camera = null;
    public GameObject Top = null;

    public Material PointCloudMaterial = null;

    public ROSTargetEnvironment TargetEnvironment;
    public string TopicName = "/simulator/sensors/lidar";
    public string AutowareTopicName = "/points_raw";
    public string ApolloTopicName = "/apollo/sensor/velodyne64/compensator/PointCloud2";

    public GameObject Vehicle = null;
    public bool Enabled = false;
    public bool ShowPointCloud = true;

    Ros.Bridge Bridge;
    uint Sequence;
    float NextSend;

    Vector4[] PointCloud;
    byte[] RosPointCloud;

    ComputeBuffer PointCloudBuffer;
    int PointCloudLayerMask;

    struct ReadRequest
    {
        public AsyncTextureReader<Vector2> Reader;
        public int Count;
        public float Angle;

        public Vector3 Origin;
        public Vector3 Start;
        public Vector3 DeltaX;
        public Vector3 DeltaY;
    }

    List<ReadRequest> Active = new List<ReadRequest>();
    Stack<AsyncTextureReader<Vector2>> Available = new Stack<AsyncTextureReader<Vector2>>();

    int CurrentIndex;
    float AngleStart;
    float AngleDelta;

    float AngleTopPart;

    float DebugStartAngle;
    int DebugCount;

    int RenderTextureWidth;
    int RenderTextureHeight;

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
        var lidarCheckbox = Vehicle.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleLidar", "Enable LIDAR:", Enabled);
        lidarCheckbox.onValueChanged.AddListener(x => Enabled = !Enabled);
    }

    void Start()
    {
        PointCloudLayerMask = 1 << LayerMask.NameToLayer("Sensor Effects");

        Camera.enabled = false;
        Camera.renderingPath = RenderingPath.Forward;
        Camera.clearFlags = CameraClearFlags.Color;
        Camera.backgroundColor = Color.black;
        Camera.allowMSAA = false;
        Camera.allowHDR = false;

        Reset();
    }

    void Reset()
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

        RenderTextureWidth = 4 * (int)(HorizontalRenderHalfAngleLimit / (360.0f / MeasurementsPerRotation));
        RenderTextureHeight = 4 * RayCount;

        int count = RayCount * MeasurementsPerRotation;
        PointCloud = new Vector4[count];
        PointCloudBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector4)));

        PointCloudMaterial.SetBuffer("PointCloud", PointCloudBuffer);

        RosPointCloud = new byte[32 * count];

        CurrentRayCount = RayCount;
        CurrentMeasurementsPerRotation = MeasurementsPerRotation;
        NextSend = 1.0f;
    }

    void Update()
    {
        if (!Enabled)
        {
            return;
        }

        var followCamera = Vehicle.GetComponent<RobotSetup>().FollowCamera;
        if (followCamera != null)
        {
            // TODO: this should be done better, without asking camera for culling mask
            ShowPointCloud = (followCamera.cullingMask & (1 << LayerMask.NameToLayer("Sensor Effects"))) != 0;
        }

        if (RayCount != CurrentRayCount || MeasurementsPerRotation != CurrentMeasurementsPerRotation)
        {
            if (RayCount > 0 && MeasurementsPerRotation >= (360.0f / HorizontalRenderHalfAngleLimit))
            {
                Reset();
            }
        }

        Camera.nearClipPlane = MinDistance;
        Camera.farClipPlane = MaxDistance;
        Camera.fieldOfView = FieldOfView;

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
            else
            {
                break;
            }
        }

        float minAngle = 360.0f / CurrentMeasurementsPerRotation;

        AngleDelta += Time.deltaTime * 360.0f * RotationFrequency;

        while (AngleDelta >= HorizontalRenderHalfAngleLimit)
        {
            float angleUse = HorizontalRenderHalfAngleLimit;

            float angleOffset = (AngleStart == 0) ? 0 : minAngle - (AngleStart % minAngle);
            //angleOffset = 0;

            int count = (int)(angleUse / minAngle);
            //count = 300;

            float diffAngle = count * minAngle;
            float leftAngle = AngleStart + angleOffset;

            float newAngle = leftAngle + diffAngle / 2.0f;
            Camera.aspect = diffAngle / FieldOfView;
            Camera.transform.localRotation = Quaternion.AngleAxis(newAngle, Vector3.up) * Quaternion.AngleAxis(CenterAngle, Vector3.right);
            Top.transform.localRotation = Quaternion.AngleAxis(newAngle, Vector3.up);

            if (count != 0)
            {
                pointCloudUpdated |= RenderLasers(count, AngleStart, angleOffset);
            }

            //DebugStartAngle = leftAngle;
            //DebugCount = count;

            AngleDelta -= angleUse;
            AngleStart += angleUse;

            if (AngleStart >= 360.0f)
            {
                AngleStart -= 360.0f;
            }
        }

        //DebugVisualize(DebugStartAngle);
        //if (DebugCount > 1)
        //{
        //    DebugVisualize(DebugStartAngle + minAngle * (DebugCount - 1));
        //}

        if (ShowPointCloud && pointCloudUpdated)
        {
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("Update Point Cloud Buffer");
#endif
            PointCloudBuffer.SetData(PointCloud);
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        NextSend -= Time.deltaTime;
        if (NextSend <= 0.0f)
        {
            SendMessage();
            NextSend = 1.0f;
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

    //void DebugVisualize(float angle)
    //{
    //    float verticalHalfAngle = FieldOfView / 2;
    //    float verticalDeltaAngle = FieldOfView / RayCount;

    //    var pos = Camera.parent.position;
    //    var rotateH = Quaternion.AngleAxis(angle, transform.up);
    //    var tr = Matrix4x4.Translate(pos) * Matrix4x4.Rotate(rotateH);

    //    for (int k = 0; k < RayCount; k++)
    //    {
    //        float a = CenterAngle - verticalHalfAngle + k * verticalDeltaAngle;

    //        var rayTr = Matrix4x4.Rotate(Quaternion.AngleAxis(a, transform.right));
    //        Vector3 direction = (tr * rayTr).MultiplyVector(Vector3.forward);

    //        Debug.DrawLine(pos, pos + direction * Camera.farClipPlane, Color.red);
    //    }
    //}

    bool RenderLasers(int count, float angle, float offset)
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
        Camera.RenderWithShader(Shader, string.Empty);
        reader.Start();

        // TODO: check if top/bottom needs to be reversed according to SystemInfo.graphicsUVStartsAtTop
        Vector3 topLeft = Camera.ViewportPointToRay(new Vector3(0, 0, 1)).direction;
        Vector3 topRight = Camera.ViewportPointToRay(new Vector3(1, 0, 1)).direction;
        Vector3 bottomLeft = Camera.ViewportPointToRay(new Vector3(0, 1, 1)).direction;

        Vector3 start = SystemInfo.graphicsUVStartsAtTop ? topLeft : bottomLeft;

        Vector3 deltaX = (topRight - topLeft) / count;
        Vector3 deltaY = (bottomLeft - topLeft) / CurrentRayCount;

        var req = new ReadRequest() {
            Reader = reader,
            Count = count,
            Angle = angle,
            Origin = Camera.transform.position,
            Start = start,
            DeltaX = deltaX,
            DeltaY = SystemInfo.graphicsUVStartsAtTop ? deltaY : -deltaY,
        };

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

        var startDir = req.Start;
        var lidarOrigin = req.Origin;

        for (int j = 0; j < CurrentRayCount; j++)
        {
            var dir = startDir;
            int y = j * RenderTextureHeight / CurrentRayCount;
            int yOffset = y * RenderTextureWidth;
            int indexOffset = j * CurrentMeasurementsPerRotation;

            for (int i = 0; i < req.Count; i++)
            {
                var direction = dir.normalized;

                int x = i * RenderTextureWidth / req.Count;

                var di = data[yOffset + x];
                float distance = di.x;
                float intensity = di.y;

                var position = lidarOrigin + direction * distance;

                int index = indexOffset + (CurrentIndex + i) % CurrentMeasurementsPerRotation;
                PointCloud[index] = distance == 0 ? Vector4.zero : new Vector4(position.x, position.y, position.z, intensity);

                // Debug.DrawLine(req.Origin, position, Color.yellow);

                dir += req.DeltaX;
            }

            startDir += req.DeltaY;
        }

        CurrentIndex = (CurrentIndex + req.Count) % CurrentMeasurementsPerRotation;

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    void SendMessage()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.BeginSample("SendMessage");
#endif

        var worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;

        if (TargetEnvironment == ROSTargetEnvironment.APOLLO)
        {
            // local.Set(local.x, local.z, local.y);
            worldToLocal = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), Vector4.zero) * worldToLocal;
        }
        else if (TargetEnvironment == ROSTargetEnvironment.AUTOWARE)
        {
            // local.Set(local.z, -local.x, local.y);
            worldToLocal = new Matrix4x4(new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(1, 0, 0, 0), Vector4.zero) * worldToLocal;
        }

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

                    var worldPos = new Vector3(point.x, point.y, point.z);
                    float intensity = point.w;

                    *(Vector3*)(ptr + offset) = worldToLocal.MultiplyPoint3x4(worldPos);
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
                frame_id = "velodyne", // needed for Autoware
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
                Base64 = System.Convert.ToBase64String(RosPointCloud, 0, count * 32),
            },
            is_dense = true,
        };

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.BeginSample("Publish");
#endif

        if (TargetEnvironment == ROSTargetEnvironment.APOLLO)
        {
            Bridge.Publish(ApolloTopicName, msg);
        }
        else if (TargetEnvironment == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.Publish(AutowareTopicName, msg);
        }
        else
        {
            Bridge.Publish(TopicName, msg);
        }
#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif

#if UNITY_EDITOR
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    void OnRenderObject()
    {
        if (Enabled && ShowPointCloud && (Camera.current.cullingMask & PointCloudLayerMask) != 0)
        {
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

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (TargetEnvironment == ROSTargetEnvironment.APOLLO)
        {
            Bridge.AddPublisher<Ros.PointCloud2>(ApolloTopicName);
        }
        else if (TargetEnvironment == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.AddPublisher<Ros.PointCloud2>(AutowareTopicName);
        }
        else
        {
            Bridge.AddPublisher<Ros.PointCloud2>(TopicName);
        }
    }
}
