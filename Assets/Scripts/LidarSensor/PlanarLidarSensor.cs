/*
* MIT License
*
* Copyright (c) 2017 Philip Tibom, Jonathan Jansson, Rickard Laurenius,
* Tobias Alldén, Martin Chemander, Sherry Davar
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

#pragma warning disable 0067, 0414, 0219

public struct VelodynePointCloudVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Color color;
    public System.UInt16 ringNumber;
    public float distance;
}

/// <summary>
/// Author: Philip Tibom
/// Simulates the lidar sensor by using ray casting.
/// </summary>
public class PlanarLidarSensor : MonoBehaviour, Ros.IRosClient
{
    public ROSTargetEnvironment targetEnv;
    public GameObject Robot = null;

    private float lastUpdate = 0;

    private Laser laser;
    private float horizontalAngle = 0;
    public float rotationSpeedHz = 10.0f;
    public float rotationAnglePerStep = 5.0f;
    public float FOVLeftLimit = -180.0f;
    public float FOVRightLimit = 180.0f;
    private float publishTimeStamp;
    public float maxRange = 40.0f;
    public float minRange = 0.0f;
    // List<VelodynePointCloudVertex> pointCloud;

    private bool isPlaying = false;

    public GameObject pointCloudObject;
    private float previousUpdate;

    private float lastLapTime;

    public string topicName = "/laser_scan";
    public string frame_id = "laser_scan_link";
    private List<float> range_array = new List<float>();
    private List<float> intensity_array = new List<float>();

    uint seqId;
    // public Transform sensorLocalspaceTransform;

    // public FilterShape filterShape;

    Ros.Bridge Bridge;

    int LidarBitmask = -1;

   
    int PointCloudIndex;
    Vector4[] PointCloud;
    ComputeBuffer PointCloudBuffer;
    public Material PointCloudMaterial = null;
    public bool ShowPointCloud = true;

    int PointCloudLayerMask;


    private int lastHitVertCount;

    void Awake()
    {
        LidarBitmask = ~(1 << LayerMask.NameToLayer("Lidar Ignore") | 1 << LayerMask.NameToLayer("Sensor Effects")) | 1 << LayerMask.NameToLayer("Lidar Only") | 1 << LayerMask.NameToLayer("PlayerConstrain");
        addUIElement();     // need to add to tweakables list before any start is called
    }

    // Use this for initialization
    private void Start()
    {
        publishTimeStamp = Time.fixedTime;
        lastLapTime = 0;


        float numberOfStepsNeededInOneLap = 360 / Mathf.Abs(rotationAnglePerStep);
        int count = (int)numberOfStepsNeededInOneLap;

        PointCloud = new Vector4[count];
        PointCloudBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector4)));

        PointCloudLayerMask = 1 << LayerMask.NameToLayer("Sensor Effects");
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1)
        {
            Bridge.AddPublisher<Ros.LaserScan>(topicName);
        }
    }

    public void Enable(bool enabled)
    {
        if (enabled)
        {
            InitiateLasers();
            lastUpdate = Time.fixedTime;
        }
        isPlaying = enabled;
    }

    private void InitiateLasers()
    {
        laser = new Laser(gameObject, maxRange);
    }

    private void Update()
    {
        if (ShowPointCloud) // TODO: only if sensor effects are enabled
        {
            PointCloudBuffer.SetData(PointCloud);
        }
    }

    private void FixedUpdate()
    {
        // Do nothing, if the simulator is paused.
        if (!isPlaying)
        {
            return;
        }

        if (Robot == null)
            Robot = transform.root.gameObject;
        var followCamera = Robot.GetComponent<AgentSetup>().FollowCamera;
        if (followCamera != null)
        {
            // TODO: this should be done better, without asking camera for culling mask
            ShowPointCloud = (followCamera.cullingMask & PointCloudLayerMask) != 0;
        }

        // Check if number of steps is greater than possible calculations by unity.
        float numberOfStepsNeededInOneLap = 360 / Mathf.Abs(rotationAnglePerStep);
        float numberOfStepsPossible = 1 / Time.fixedDeltaTime / rotationSpeedHz;

        // Check if it is time to step. Example: 2hz = 2 rotations in a second.
        var neededInterval = 1f / (numberOfStepsNeededInOneLap * rotationSpeedHz);
        var diff = Time.fixedTime - lastUpdate;
        if (diff > neededInterval)
        {
            int precalculateIterations = (int)(diff / neededInterval);
            for (int i = 0; i < precalculateIterations; i++)
            {
                // Perform rotation.
                transform.Rotate(0, rotationAnglePerStep, 0, Space.Self);

                var worldPoint = new Vector4();
                // Fire laser.
                RaycastHit hit = laser.ShootRay(LidarBitmask);
                if (hit.distance < minRange)
                {
                    range_array.Insert(0, 0.0f);
                }
                else
                {
                    worldPoint.x = hit.point.x;
                    worldPoint.y = hit.point.y;
                    worldPoint.z = hit.point.z;
                    range_array.Insert(0, hit.distance);
                }
                Color c;
                Renderer rend = hit.transform?.GetComponent<Renderer>();
                if (rend != null && (hit.collider as MeshCollider) != null && rend.sharedMaterial != null && rend.sharedMaterial.mainTexture != null)
                {
                    Texture2D tex = rend.sharedMaterial.mainTexture as Texture2D; //Can be improved later dealing with multiple share materials
                    Vector2 pixelUV = hit.textureCoord;
                    c = tex.GetPixelBilinear(pixelUV.x, pixelUV.y);
                }
                else
                {
                    c = Color.black;
                }
                intensity_array.Insert(0, c.grayscale * 255);
                worldPoint.w = c.grayscale;

                PointCloud[PointCloudIndex] = worldPoint;
                PointCloudIndex = (PointCloudIndex + 1) % PointCloud.Length;

                horizontalAngle += rotationAnglePerStep; // Keep track of our current rotation.
                if (horizontalAngle >= 360)
                {
                    horizontalAngle -= 360;
                    lastLapTime = Time.fixedTime;
                    publishTimeStamp = Time.fixedTime;

                    SendPointCloud();
                }

                // Update current execution time.
                lastUpdate += neededInterval;
            }
        }
    }

    void OnRenderObject()
    {
        if (ShowPointCloud && (Camera.current.cullingMask & PointCloudLayerMask) != 0)
        {
            PointCloudMaterial.SetPass(0);
            PointCloudMaterial.SetBuffer("PointCloud", PointCloudBuffer);
            Graphics.DrawProcedural(MeshTopology.Points, PointCloud.Length);
        }
    }
    void SendPointCloud()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }
        int FOV_take_1 = (int)(Mathf.Abs(FOVLeftLimit - FOVRightLimit) / Mathf.Abs(rotationAnglePerStep));
        int FOV_skip = (int)(Mathf.Abs(-270 - FOVLeftLimit) / Mathf.Abs(rotationAnglePerStep));
        int FOV_take_2 = 0;
        if (FOV_take_1 + FOV_skip != (int)(360/rotationAnglePerStep))
        {
            FOV_take_2 = (int)(360/rotationAnglePerStep) - FOV_skip - FOV_take_1;
        } 


        var msg = new Ros.LaserScan()
        {
            header = new Ros.Header()
            {
                stamp = Ros.Time.Now(),
                seq = seqId++,
                frame_id = frame_id,
            },
            angle_min = FOVLeftLimit*Mathf.PI/180.0f,
            angle_max = FOVRightLimit*Mathf.PI/180.0f,
            angle_increment = rotationAnglePerStep*Mathf.PI/180.0f,
            time_increment = 1.0f*360.0f/(rotationSpeedHz*rotationAnglePerStep),
            scan_time = 1.0f/rotationSpeedHz,
            range_min = minRange,
            range_max = maxRange,
            ranges = (range_array.Take(FOV_take_2).Concat(range_array.Skip(FOV_skip).Take(FOV_take_1))).ToArray(),
            intensities = (intensity_array.Take(FOV_take_2).Concat(intensity_array.Skip(FOV_skip).Take(FOV_take_1))).ToArray(),
        };

        if (targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1)
        {
            Bridge.Publish(topicName, msg);
        }

        range_array.Clear();
        intensity_array.Clear();
    }

    private void addUIElement()
    {
        var lidarCheckbox = Robot.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleLidar", "Enable LIDAR:", false);
        lidarCheckbox.onValueChanged.AddListener(x => Enable(x));
    }
}