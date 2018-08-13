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
    private Dictionary<Collider, Vector3> radarDetectedColliders;
    private HashSet<Collider> exclusionColliders;

    Ros.Bridge Bridge;
    public string ApolloTopicName = "/apollo/sensor/conti_radar";
    const float publishInterval = 1 / 13.4f; // 1/HZ
    private float publishTimer = 0;
    private List<Collider> utilColList = new List<Collider>();

    private static System.DateTime originTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    private int seqId = 0;
    Ros.drivers.ContiRadarObs[] fixedRadarObjArr = new Ros.drivers.ContiRadarObs[100];

    private bool enabled = false;

    void Start()
    {
        foreach (var rrt in radarRangeTriggers)
        {
            rrt.SetCallback(OnObjDetected);
        }
        radarDetectedColliders = new Dictionary<Collider, Vector3>();
        var robot = GetComponentInParent<RobotSetup>();
        if (robot != null)
        {
            exclusionColliders = new HashSet<Collider>(new List<Collider>(robot.GetComponentsInChildren<Collider>()));
        }

        Enable(false);
    }

    private void OnDrawGizmos()
    {
        if (!enabled)
        {
            return;
        }

        if (!visualizeDetectionGizmo)
        {
            return;
        }

        if (radarDetectedColliders != null)
        {
            foreach (var key in radarDetectedColliders.Keys)
            {
                Vector3 point = radarDetectedColliders[key];
                Gizmos.matrix = Matrix4x4.TRS(point, transform.rotation, Vector3.one);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }

    void FixedUpdate()
    {
        if (!enabled)
        {
            return;
        }

        if (Time.fixedTime - publishTimer > publishInterval)
        {
            publishTimer += publishInterval;
            SendRadarData();
            radarDetectedColliders.Clear();
        }

        if (radarDetectedColliders != null)
        {
            utilColList.Clear();
            utilColList.AddRange(radarDetectedColliders.Keys);
            foreach (var col in utilColList)
            {
                Vector3 point = col.ClosestPoint(transform.position);
                radarDetectedColliders[col] = point;
            }
        }
    }

    public void OnObjDetected(Collider other)
    {
        if (!radarDetectedColliders.ContainsKey(other) && !exclusionColliders.Contains(other) && !IsConcaveMeshCollider(other))
        {
            radarDetectedColliders.Add(other, Vector3.zero);
        }
    }

    public void Enable(bool enabled)
    {
        this.enabled = enabled;

        radarRangeTriggers.ForEach(t => t.gameObject.SetActive(enabled));
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
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

        //Debug.Log("Publishing Radar");
        var apolloHeader = new Ros.ApolloHeader()
        {
            timestamp_sec = (System.DateTime.UtcNow - originTime).TotalSeconds,
            module_name = "conti_radar",
            sequence_num = seqId
        };

        var radarPos = transform.position;
        var radarAim = transform.forward;
        var radarRight = transform.right;

        int num = 0;
        foreach (var pair in radarDetectedColliders)
        {
            if (num > 99)
            {
                break;
            }
            Collider col = pair.Key;
            Vector3 point = pair.Value;
            Vector3 relPos = point - radarPos;
            Vector3 relVel = col.attachedRigidbody.velocity;

            var matrix = Matrix4x4.TRS(point, transform.rotation, Vector3.one);

            fixedRadarObjArr[num] = new Ros.drivers.ContiRadarObs()
            {
                header = apolloHeader,
                clusterortrack = false,
                obstacle_id = num,
                longitude_dist = Vector3.Project(relPos, radarAim).magnitude,
                lateral_dist = Vector3.Project(relPos, radarRight).magnitude,
                longitude_vel = Vector3.Project(relVel, radarAim).magnitude,
                lateral_vel = Vector3.Project(relVel, radarRight).magnitude,
                rcs = 11.0, //
                dynprop = 1, // seem to be constant
                longitude_dist_rms = 0.371,
                lateral_dist_rms = 0.478,
                longitude_vel_rms = 0.371,
                lateral_vel_rms = 0.616,
                probexist = 1.0, //prob confidence
                meas_state = 2, //
                longitude_accel = .0, 
                lateral_accel = .0,
                oritation_angle = .0,
                longitude_accel_rms = 0.794,
                lateral_accel_rms = 0.005,
                oritation_angle_rms = 1.909,
                length = 2.0,
                width = 2.4,
                obstacle_class = 1, // single type but need to find car number
            };
            ++num;
        }

        var msg = new Ros.drivers.ContiRadar
        {
            header = apolloHeader,
            contiobs = fixedRadarObjArr,
            object_list_status = new Ros.drivers.ObjectListStatus_60A
            {
                nof_objects = num,
                meas_counter = 22800,
                interface_version = 0
            }
        };

        Bridge.Publish(ApolloTopicName, msg);

        ++seqId;
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.drivers.ContiRadar>(ApolloTopicName);        
    }
}
