/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class RadarSensor : MonoBehaviour
{
    public bool visualizeDetectionGizmo = false;
    public List<RadarRangeTrigger> radarRangeTriggers;
    private HashSet<Collider> detectedColliders;
    private HashSet<Collider> exclusionColliders;
    private float visualizationRefreshRate = 20f; //HZ
    private float visualizationTimer = 0f;

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
            //Debug.Log(detectedColliders.Count);
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
}
