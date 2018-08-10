/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class RadarSensor : MonoBehaviour, Ros.IRosClient
{
    public bool visualizeDetectionGizmo = false;
    public List<RadarRangeTrigger> radarRangeTriggers;
    private HashSet<Collider> detectedColliders;
    private HashSet<Collider> exclusionColliders;
    private float visualizationRefreshRate = 20f; //HZ
    private float visualizationTimer = 0f;

    Ros.Bridge Bridge;
    public string ApolloTopicName = "/apollo/sensor/conti_radar";
    const float publishInterval = 1 / 10.0f; // 1 / HZ
    private float publishTimer = 0;

    void Start()
    {
        foreach (var rrt in radarRangeTriggers)
        {
            rrt.SetCallback(OnColliderDetected);
        }
        detectedColliders = new HashSet<Collider>();
        var robot = GetComponentInParent<RobotSetup>();
        if (robot != null)
        {
            exclusionColliders = new HashSet<Collider>(new List<Collider>(robot.GetComponentsInChildren<Collider>()));
        }
    }

    public void OnColliderDetected(Collider other)
    {
        if (!detectedColliders.Contains(other))
        {
            detectedColliders.Add(other);
        }
    }

    private void OnDrawGizmos()
    {
        if (!visualizeDetectionGizmo)
        {
            return;
        }

        if (detectedColliders != null)
        {
            foreach (var col in detectedColliders)
            {
                if (exclusionColliders.Contains(col))
                {
                    continue;
                }
                
                if (IsConcaveMeshCollider(col))
                {
                    continue;
                }
                Vector3 point = col.ClosestPoint(transform.position);
                Gizmos.matrix = Matrix4x4.TRS(point, transform.rotation, Vector3.one);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
            if (Time.time - visualizationTimer > 1f / visualizationRefreshRate)
            {
                detectedColliders.Clear();
                visualizationTimer = Time.time;
            }
        }
    }

    void FixedUpdate()
    {
        if (Time.fixedTime - publishTimer > publishInterval)
        {
            publishTimer += Time.fixedTime;
            SendRadarData();
        }
        publishTimer += Time.fixedDeltaTime;
    }

    bool IsConcaveMeshCollider(Collider col)
    {
        var meshCol = col as MeshCollider;
        if (meshCol != null)
        {
            if (!meshCol.convex)
            {
                return true;
            }
        }
        return false;
    }

    public void SendRadarData()
    {

    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.PointCloud2>(ApolloTopicName);        
    }
}
