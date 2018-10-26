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
    public struct ObjectTrackInfo
    {
        public int id;
        public Vector3 point;
        public bool newDetection;
    }

    public bool visualizeDetectionGizmo = false;
    public List<RadarRangeTrigger> radarRangeTriggers;
    private HashSet<Collider> exclusionColliders;

    const int maxObjs = 100;

    private Dictionary<Collider, ObjectTrackInfo> radarDetectedColliders = new Dictionary<Collider, ObjectTrackInfo>();
    private Dictionary<Collider, ObjectTrackInfo> lastColliders = new Dictionary<Collider, ObjectTrackInfo>();
    private Utils.MinHeap IDHeap = new Utils.MinHeap(maxObjs);

    public LayerMask radarBlockers;

    Ros.Bridge Bridge;
    const float detectInterval = 1 / 20.0f; // 1/HZ
    public string ApolloTopicName = "/apollo/sensor/conti_radar";
    const float publishInterval = 1 / 13.4f; // 1/HZ
    private float publishTimer = 0;
    private List<Collider> utilColList = new List<Collider>();

    private static System.DateTime originTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    private uint seqId = 0;
    private List<Ros.Apollo.Drivers.ContiRadarObs> radarObjList = new List<Ros.Apollo.Drivers.ContiRadarObs>(maxObjs);

    private bool isEnabled = false;

    void Start()
    {
        foreach (var rrt in radarRangeTriggers)
        {
            rrt.SetCallback(OnObjectDetected);
        }
        var robot = GetComponentInParent<RobotSetup>();
        if (robot != null)
        {
            exclusionColliders = new HashSet<Collider>(new List<Collider>(robot.GetComponentsInChildren<Collider>()));
        }

        IDHeap = new Utils.MinHeap(maxObjs);

        for (int i = 0; i < maxObjs; i++)
        {
            IDHeap.Add(i);
        }

        Enable(false);
    }

    private void OnDrawGizmos()
    {
        if (!isEnabled)
        {
            return;
        }

        if (!visualizeDetectionGizmo)
        {
            return;
        }

        foreach (var pair in radarDetectedColliders)
        {
            if (pair.Key == null)
            {
                continue;
            }
            Gizmos.matrix = Matrix4x4.TRS(radarDetectedColliders[pair.Key].point, transform.rotation, Vector3.one);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }

    void FixedUpdate()
    {
        if (!isEnabled)
        {
            return;
        }

        if (Time.fixedTime - publishTimer > publishInterval)
        {
            publishTimer += publishInterval;
            TrackObstableStates();
            SendRadarData();
            RememberLastColliders();
            radarDetectedColliders.Clear();
        }
    }

    //linked to all trigger stay callbacks
    public void OnObjectDetected(Collider detect)
    {
        if (!radarDetectedColliders.ContainsKey(detect) && !exclusionColliders.Contains(detect) && !IsConcaveMeshCollider(detect) && radarDetectedColliders.Count < maxObjs)
        {
            RaycastHit hit;
            var orig = transform.position;
            var dir = transform.forward;
            var end = detect.bounds.center;
            end.Set(end.x, orig.y, end.z);
            var dist = (orig - end).magnitude;
            if (Physics.Raycast(orig, dir, out hit, dist, radarBlockers.value) && hit.collider != detect) //If roughly blocked, don't consider
            {
                return;
            }
            Vector3 point = detect.ClosestPoint(transform.position);
            dist = (orig - new Vector3(point.x, orig.y, point.z)).magnitude - 0.001f;
            if (Physics.Raycast(orig, dir, dist, radarBlockers.value)) //If not blocked but closest point blocked, us previous hit point
            {
                point = hit.point;
            }
            radarDetectedColliders.Add(detect, new ObjectTrackInfo() { id = -1, point = point }); //add only if the object is not blocked
        }
    }

    void TrackObstableStates()
    {
        //track col between two radar sends, if one collider is still detected, track id if become undetected, recycle id
        utilColList.Clear();
        utilColList.AddRange(lastColliders.Keys);
        utilColList.RemoveAll(c => c == null);
        for (int i = 0; i < utilColList.Count; i++)
        {
            Collider col = utilColList[i];
            if (radarDetectedColliders.ContainsKey(col))
            {
                radarDetectedColliders[col] = new ObjectTrackInfo() { id = lastColliders[col].id, point = radarDetectedColliders[col].point };
            }
            else
            {                
                //put id back to min heap
                IDHeap.Add(lastColliders[col].id);
                //Debug.Log("id " + lastColliders[col].id + " has been put back");
            }
        }

        //if one collider become detected, get min available id
        utilColList.Clear();
        utilColList.AddRange(radarDetectedColliders.Keys);
        utilColList.RemoveAll(c => c == null);
        for (int i = 0; i < utilColList.Count; i++)
        {
            Collider col = utilColList[i];
            if (!lastColliders.ContainsKey(col))
            {
                //get id out of min heap
                if (IDHeap.Size < 1)
                {
                    Debug.Log($"{nameof(IDHeap)} size become empty, logic error.");
                }
                
                radarDetectedColliders[col] = new ObjectTrackInfo() { id = IDHeap.Pop(), point = radarDetectedColliders[col].point, newDetection = true };
                
                //Debug.Log("get new available id " + a + " in heap and assign");
            }
        }
    }

    public void SendRadarData()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

        var apolloHeader = new Ros.ApolloHeader()
        {
            timestamp_sec = (System.DateTime.UtcNow - originTime).TotalSeconds,
            module_name = "conti_radar",
            sequence_num = seqId
        };

        var radarPos = transform.position;
        var radarAim = transform.forward;
        var radarRight = transform.right;

        radarObjList.Clear();

        utilColList.Clear();
        utilColList.AddRange(radarDetectedColliders.Keys);
        utilColList.RemoveAll(c => c == null);

        //Debug.Log("radarDetectedColliders.Count: " + radarDetectedColliders.Count);

        for (int i = 0; i < utilColList.Count; i++)
        {
            Collider col = utilColList[i];
            Vector3 point = radarDetectedColliders[col].point;
            Vector3 relPos = point - radarPos;
            Vector3 relVel = col.attachedRigidbody == null ? Vector3.zero : col.attachedRigidbody.velocity;

            //Debug.Log("id to be assigned to obstacle_id is " + radarDetectedColliders[col].id);

            radarObjList.Add(new Ros.Apollo.Drivers.ContiRadarObs()
            {
                header = apolloHeader,
                clusterortrack = false,
                obstacle_id = radarDetectedColliders[col].id,
                longitude_dist = Vector3.Project(relPos, radarAim).magnitude,
                lateral_dist = Vector3.Project(relPos, radarRight).magnitude,
                longitude_vel = Vector3.Project(relVel, radarAim).magnitude,
                lateral_vel = Vector3.Project(relVel, radarRight).magnitude,
                rcs = 11.0, //
                dynprop = 0, // seem to be constant
                longitude_dist_rms = 0,
                lateral_dist_rms = 0,
                longitude_vel_rms = 0,
                lateral_vel_rms = 0,
                probexist = 1.0, //prob confidence
                meas_state = radarDetectedColliders[col].newDetection ? 1 : 2, //1 new 2 exist
                longitude_accel = 0,
                lateral_accel = 0,
                oritation_angle = 0,
                longitude_accel_rms = 0,
                lateral_accel_rms = 0,
                oritation_angle_rms = 0,
                length = 2.0,
                width = 2.4,
                obstacle_class = 1, // single type but need to find car number
            });

        }

        var msg = new Ros.Apollo.Drivers.ContiRadar
        {
            header = apolloHeader,
            contiobs = radarObjList,
            object_list_status = new Ros.Apollo.Drivers.ObjectListStatus_60A
            {
                nof_objects = utilColList.Count,
                meas_counter = 22800,
                interface_version = 0
            }
        };

        Bridge.Publish(ApolloTopicName, msg);

        ++seqId;
    }

    void RememberLastColliders()
    {
        lastColliders.Clear();
        foreach (var pair in radarDetectedColliders)
        {
            if (pair.Key != null)
            {
                lastColliders.Add(pair.Key, new ObjectTrackInfo() { id = pair.Value.id , point = pair.Value.point });
            }
        }
    }

    public void Enable(bool enabled)
    {
        isEnabled = enabled;

        radarRangeTriggers.ForEach(t => {
            t.gameObject.SetActive(enabled);
            t.gameObject.GetComponent<MeshRenderer>().enabled = enabled;
        });

        if (enabled)
        {
            publishTimer = Time.fixedTime;
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

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.Apollo.Drivers.ContiRadar>(ApolloTopicName);        
    }
}
