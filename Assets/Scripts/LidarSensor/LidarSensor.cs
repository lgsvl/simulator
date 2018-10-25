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
public class LidarSensor : MonoBehaviour, Ros.IRosClient
{
    public ROSTargetEnvironment targetEnv;
    private float lastUpdate = 0;

    private List<Laser> lasers;
    private float horizontalAngle = 0;

    public int numberOfLasers = 2;
    public float rotationSpeedHz = 1.0f;
    public float rotationAnglePerStep = 45.0f;
    public float publishInterval = 1f;
    private float publishTimeStamp;
    public float rayDistance = 100f;
    public float upperFOV = 30f;
    public float lowerFOV = 30f;
    public float offset = 0.001f;
    public float upperNormal = 30f;
    public float lowerNormal = 30f;
    List<VelodynePointCloudVertex> pointCloud;

    public float lapTime = 0;

    private bool isPlaying = false;

    public GameObject pointCloudObject;
    private float previousUpdate;

    private float lastLapTime;

    public GameObject lineDrawerPrefab;

    public string topicName = "/points_raw";
    public string ApolloTopicName = "/apollo/sensor/velodyne64/compensator/PointCloud2";

    uint seqId;
    public float exportScaleFactor = 1.0f;
    public Transform sensorLocalspaceTransform;

    public FilterShape filterShape;

    Ros.Bridge Bridge;

    int LidarBitmask = -1;

    // lidar effects
    public bool displayHitFx;
    public bool isShaderEffect = true;
    public GameObject lidarPfxPrefab;
    private List<Vector3> hitPositions = new List<Vector3>();
    private ParticleSystem lidarPfxSystem;
    private bool lidarPfxNeedsUpdate = true;

    public Material lidarshaderEffectMat;
    private GameObject pcMeshGO;
    private MeshFilter mf;
    private MeshRenderer mr;
    private const int vertexLimitPerMesh = 65535;
    private Mesh pointCloudMesh;
    private List<Vector3> pcVertices = new List<Vector3>();
    private List<Vector3> pcNormals = new List<Vector3>();
    private int[] pcIndices = new int[vertexLimitPerMesh];
    private int lastHitVertCount;

    void Awake()
    {
        LidarBitmask = ~(1 << LayerMask.NameToLayer("Lidar Ignore") | 1 << LayerMask.NameToLayer("Sensor Effects")) | 1 << LayerMask.NameToLayer("Lidar Only") | 1 << LayerMask.NameToLayer("PlayerConstrain");
    }

    // Use this for initialization
    private void Start()
    {
        publishTimeStamp = Time.fixedTime;
        lastLapTime = 0;
        pointCloud = new List<VelodynePointCloudVertex>();
        //publishInterval = 1f / rotationSpeedHz;
        pointCloudMesh = new Mesh();
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.AddPublisher<Ros.PointCloud2>(topicName);
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.AddPublisher<Ros.PointCloud2>(ApolloTopicName);
        }
    }

    public void Enable(bool enabled)
    {
        if (enabled)
        {
            InitiateLasers();
            lastUpdate = Time.fixedTime;
            ToggleLidarEffect(true);
        }
        else
        {
            DeleteLasers();
            ToggleLidarEffect(false);
        }
        isPlaying = enabled;
    }

    private void InitiateLasers()
    {
        // Initialize number of lasers, based on user selection.
        DeleteLasers();

        float upperTotalAngle = upperFOV / 2;
        float lowerTotalAngle = lowerFOV / 2;
        float upperAngle = upperFOV / (numberOfLasers / 2);
        float lowerAngle = lowerFOV / (numberOfLasers / 2);
        offset = (offset / 100) / 2; // Convert offset to centimeters.
        for (int i = 0; i < numberOfLasers; i++)
        {
            GameObject lineDrawer = Instantiate(lineDrawerPrefab);
            lineDrawer.transform.parent = gameObject.transform; // Set parent of drawer to this gameObject.
            if (i < numberOfLasers / 2)
            {
                lasers.Add(new Laser(gameObject, lowerTotalAngle + lowerNormal, rayDistance, -offset, lineDrawer, i));

                lowerTotalAngle -= lowerAngle;
            }
            else
            {
                lasers.Add(new Laser(gameObject, upperTotalAngle - upperNormal, rayDistance, offset, lineDrawer, i));
                upperTotalAngle -= upperAngle;
            }
        }
    }

    private void DeleteLasers()
    {
        if (lasers != null)
        {
            foreach (Laser l in lasers)
            {
                Destroy(l.GetRenderLine().gameObject);
            }
        }

        lasers = new List<Laser>();
    }

    private void Update()
    {
        // Bug Fix in 2018.3 TODO Eric test
        if (displayHitFx)
        {
            VisualizeLidarPfx();
        }
    }

    private void FixedUpdate()
    {
        // Do nothing, if the simulator is paused.
        if (!isPlaying)
        {
            return;
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

                // Execute lasers.
                for (int x = 0; x < lasers.Count; x++)
                {
                    RaycastHit hit = lasers[x].ShootRay(LidarBitmask);
                    float distance = hit.distance;
                    if (distance != 0 && (filterShape == null || !filterShape.Contains(hit.point))) // Didn't hit anything or in filter shape, don't add to list.
                    {
                        //float verticalAngle = lasers[x].GetVerticalAngle();
                        Color c;
                        Renderer rend = hit.transform.GetComponent<Renderer>();
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

                        pointCloud.Add(new VelodynePointCloudVertex()
                        {
                            position = hit.point,
                            normal = hit.normal,
                            ringNumber = (System.UInt16)x,
                            distance = distance,
                            color = c,
                        });
                    }
                }

                horizontalAngle += rotationAnglePerStep; // Keep track of our current rotation.
                if (horizontalAngle >= 360)
                {
                    horizontalAngle -= 360;
                    lastLapTime = Time.fixedTime;
                    publishTimeStamp = Time.fixedTime;

                    if (displayHitFx)
                    {
                        if (pcMeshGO == null)
                        {
                            pcMeshGO = new GameObject("LidarPointCloudMesh");
                            pcMeshGO.layer = LayerMask.NameToLayer("Sensor Effects");
                            mf = pcMeshGO.AddComponent<MeshFilter>();
                            mr = pcMeshGO.AddComponent<MeshRenderer>();
                        }

                        // Bug Fix in 2018.3 TODO Eric test
                        if (!isShaderEffect && lidarPfxNeedsUpdate)
                        {
                            for (int j = 0; j < pointCloud.Count; j++)
                            {
                                hitPositions.Add(pointCloud[j].position);
                            }
                            lidarPfxNeedsUpdate = false;
                        }

                        BuildLidarHitMesh(pointCloud);
                    }
                    else
                    {
                        if (pcMeshGO != null)
                        {
                            Destroy(pcMeshGO);
                        }
                    }

                    SendPointCloud(pointCloud);
                    pointCloud.Clear();
                }

                // Update current execution time.
                lastUpdate += neededInterval;
            }
        }
    }

    // Bug Fix in 2018.3 TODO Eric test
    private void ToggleLidarEffect(bool enabled)
    {
        if (enabled)
        {
            if (isShaderEffect)
            {
                if (pcMeshGO == null)
                {
                    pcMeshGO = new GameObject("LidarPointCloudMesh");
                    pcMeshGO.layer = LayerMask.NameToLayer("Sensor Effects");
                    mf = pcMeshGO.AddComponent<MeshFilter>();
                    mr = pcMeshGO.AddComponent<MeshRenderer>();
                }
            }
            else
            {
                lidarPfxSystem = Instantiate(lidarPfxPrefab, transform.root).GetComponent<ParticleSystem>();
            }
        }
        else
        {
            if (isShaderEffect)
            {
                Destroy(pcMeshGO);
            }
            else
            {
                Destroy(lidarPfxSystem.gameObject);
            }
        }
    }
    
    private void VisualizeLidarPfx()
    {
        if (isShaderEffect) return;

        if (hitPositions.Count == 0) return;

        var main = lidarPfxSystem.main;
        main.maxParticles = hitPositions.Count;
        short pcCount = (short)hitPositions.Count;

        var emission = lidarPfxSystem.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, pcCount) });

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[hitPositions.Count];
        int count = lidarPfxSystem.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle particle = particles[i];
            particle.position = hitPositions[i];
            particles[i] = particle;
        }
        lidarPfxSystem.Clear();
        lidarPfxSystem.SetParticles(particles, hitPositions.Count);
        lidarPfxSystem.Play();
        hitPositions.Clear();
        lidarPfxNeedsUpdate = true;
    }

    //right now only support one mesh
    private void BuildLidarHitMesh(List<VelodynePointCloudVertex> pcHit)
    {
        if (!isShaderEffect) return;

        int hitVertCount = pcHit.Count;
        if (hitVertCount > 0)
        {
            hitVertCount = hitVertCount > vertexLimitPerMesh ? vertexLimitPerMesh : hitVertCount;

            pointCloudMesh.Clear();

            pcVertices.Clear();
            pcNormals.Clear();

            for (int i = 0; i < hitVertCount; i++)
            {
                var adjustedIndex = i + (pcHit.Count - hitVertCount);
                pcVertices.Add(pcHit[adjustedIndex].position);
                pcNormals.Add((transform.position - pcHit[adjustedIndex].position).normalized); //use lidar aim vector as normal    
                pcIndices[i] = i;
            }

            //clean extraneous indices
            if (hitVertCount < lastHitVertCount)
            {
                for (int i = hitVertCount; i < Mathf.Min(lastHitVertCount, vertexLimitPerMesh); i++)
                {
                    pcIndices[i] = 0;
                }
            }

            pointCloudMesh.SetVertices(pcVertices);
            pointCloudMesh.SetNormals(pcNormals);
            pointCloudMesh.SetIndices(pcIndices, MeshTopology.Points, 0);

            if (mf != null)
            {
                mf.sharedMesh = pointCloudMesh;
            }

            if (mr != null)
            {
                mr.sharedMaterial = lidarshaderEffectMat;
            }

            lastHitVertCount = hitVertCount;
        }
    }

    void SendPointCloud(List<VelodynePointCloudVertex> pointCloud)
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

        var pointCount = pointCloud.Count;
        byte[] byteData = new byte[32 * pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var local = sensorLocalspaceTransform.InverseTransformPoint(pointCloud[i].position);
            if (targetEnv == ROSTargetEnvironment.AUTOWARE)
            {
                local.Set(local.z, -local.x, local.y);
            }
            else
            {
                local.Set(local.x, local.z, local.y);
            }

            var scaledPos = local * exportScaleFactor;
            var x = System.BitConverter.GetBytes(scaledPos.x);
            var y = System.BitConverter.GetBytes(scaledPos.y);
            var z = System.BitConverter.GetBytes(scaledPos.z);
            //var intensity = System.BitConverter.GetBytes(pointCloud[i].color.maxColorComponent);
            //var intensity = System.BitConverter.GetBytes((float)(((int)pointCloud[i].color.r) << 16 | ((int)pointCloud[i].color.g) << 8 | ((int)pointCloud[i].color.b)));

            //var intensity = System.BitConverter.GetBytes((byte)pointCloud[i].distance);
            var intensity = System.BitConverter.GetBytes((byte)(pointCloud[i].color.grayscale * 255));

            var ring = System.BitConverter.GetBytes(pointCloud[i].ringNumber);

            var ts = System.BitConverter.GetBytes((double)0.0);

            System.Buffer.BlockCopy(x, 0, byteData, i * 32 + 0, 4);
            System.Buffer.BlockCopy(y, 0, byteData, i * 32 + 4, 4);
            System.Buffer.BlockCopy(z, 0, byteData, i * 32 + 8, 4);
            System.Buffer.BlockCopy(intensity, 0, byteData, i * 32 + 16, 1);
            System.Buffer.BlockCopy(ts, 0, byteData, i * 32 + 24, 8);
        }

        var msg = new Ros.PointCloud2()
        {
            header = new Ros.Header()
            {
                stamp = Ros.Time.Now(),
                seq = seqId++,
                frame_id = "velodyne", // needed for Autoware
            },
            height = 1,
            width = (uint)pointCount,
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
            row_step = (uint)pointCount * 32,
            data = byteData,
            is_dense = true,
        };

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.Publish(topicName, msg);
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.Publish(ApolloTopicName, msg);
        }
    }
}
