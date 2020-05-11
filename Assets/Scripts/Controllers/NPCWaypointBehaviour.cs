/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Utilities;

public class NPCWaypointBehaviour : NPCBehaviourBase
{
    #region vars
    // map data
    public MapLane currentMapLane;
    public List<float> laneSpeed; // used for waypoint mode
    public List<Vector3> laneData;
    public List<Vector3> laneAngle;
    public List<float> laneIdle;
    public List<float> laneTime;
    public List<bool> laneDeactivate;
    public List<float> laneTriggerDistance;
    public bool waypointLoop;

    // targeting
    private Vector3 currentTarget;
    private Vector3 currentTargetDirection;
    private int currentIndex = 0;

    public float currentIdle = 0f;
    public bool currentDeactivate = false;

    private double wakeUpTime;
    [System.NonSerialized]
    static public Dictionary<uint, List<string>> logWaypoint = new Dictionary<uint, List<string>>();
    private bool doneLog = false;
    private bool activateNPC = false;

    // State kept for showing first running for one NPC
    private bool isFirstRun = true;
    // State kept for showing waypoints updated from API
    private bool updatedWaypoints = false;
    // State kept for checking for reaching to waypoint
    private bool checkReachBlocked = false;

    private Coroutine idleCoroutine;

    // Waypoint Driving
    private enum WaypointDriveState
    {
        Wait,
        Drive,
        Despawn
    };
    WaypointDriveState waypointDriveState = WaypointDriveState.Wait;

    public enum NPCWaypointState
    {
        None,
        Driving,
        Idle,
        AwaitingTrigger
    };
    #endregion

    #region mono
    public override void PhysicsUpdate()
    {
        if (!rb.isKinematic)
        {
            rb.isKinematic = true;
        }
        if (!controller.MainCollider.isTrigger)
        {
            controller.MainCollider.isTrigger = true;
        }

        controller.SetBrakeLights(currentSpeed < 2.0f);
        
        NPCProcessIdleTime();
        if (isFirstRun && currentIndex == 0 && updatedWaypoints)
        {
            // The NPC add current pose and moves to initial waypoint.
            AddPoseToFirstWaypoint();
        }
        if (activateNPC)
        {
            NPCNextMove();
        }
    }

    private void AddPoseToFirstWaypoint()
    {
        laneData.Insert(0, transform.position);
        laneAngle.Insert(0, transform.eulerAngles);
        laneSpeed.Insert(0, 0f);
        laneTime.Insert(0, 0);
        laneIdle.Insert(0, 0);
        laneDeactivate.Insert(0, false);

        float initialMoveDuration = (laneData[1] - laneData[0]).magnitude / laneSpeed[1];

        for (int i=1; i<laneTime.Count; i++)
        {
            laneTime[i] += initialMoveDuration;
        }

        updatedWaypoints = false;
        isFirstRun = false;
        checkReachBlocked = false;
    }

    // After elapsedTime, DebugMsg will save recorded data into file.
    // This function should be called in PhysicsUpdate()
    private void DebugMsg(float elapsedTime)
    {
        if (SimulatorManager.Instance.CurrentTime - SimulatorManager.Instance.NPCManager.startTime > elapsedTime)
        {
            if (!doneLog)
            {
                WriteMsg();
                doneLog = true;
            }
        }
    }

    // This function should be placed at the place where you want to record variables.
    private void DebugMsg()
    {
        if (currentIndex >= laneData.Count - 1)
            return;

        var t = SimulatorManager.Instance.CurrentTime;
        string logMsg = "";
        if (laneTime.Count > 0)
            logMsg = $"NPC{GTID}, Idx: {currentIndex}, pose: {laneData[currentIndex]}, angle: {laneAngle[currentIndex]}, " +
            $"laneTime: {laneTime[currentIndex]}, time: {t}, rel_t: {t - wakeUpTime}";

        if (!logWaypoint.ContainsKey(GTID))
            logWaypoint.Add(GTID, new List<string>());
        else
            logWaypoint[GTID].Add(logMsg);
    }

    private void WriteMsg()
    {
        string filename = "/data/Simulator/wp_waypoints.txt";

        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(filename))
        {
            foreach (var numNPC in logWaypoint.Keys)
            {
                file.WriteLine($"NPC {numNPC}");
                foreach (var oneMsg in logWaypoint[numNPC])
                    file.WriteLine(oneMsg);
            }
        }

        Debug.Log($"Finished Write log to file.");
    }
    #endregion

    #region init
    public override void Init(int seed)
    {
        ResetData();
    }

    public override void InitLaneData(MapLane lane)
    {
        ResetData();
        currentMapLane = lane;
        SetLaneData(currentMapLane.mapWorldPositions);
    }
    #endregion

    #region spawn
    private void ResetData()
    {
        if(idleCoroutine != null) FixedUpdateManager.StopCoroutine(idleCoroutine);
        currentMapLane = null;
        controller.ResetLights();
        currentSpeed = 0f;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
    }
    #endregion

    #region physics
    private void NPCNextMove()
    {
        Vector3 position;
        Quaternion rotation;
        float time = (float)(SimulatorManager.Instance.CurrentTime - wakeUpTime);

        if (waypointDriveState == WaypointDriveState.Despawn)
        {
            return;
        }
        else if (waypointDriveState == WaypointDriveState.Wait)
        {
            return;
        }

        if (waypointDriveState != WaypointDriveState.Drive)
        {
            return;
        }

        if (currentIndex < laneData.Count-1)
        {
            // Wait for current time synced with waypoint time
            if (time < laneTime[currentIndex])
            {
                return;
            }
            // Check proximity of current pose to currentIndex+1 index waypoint.
            var distance2 = Vector3.SqrMagnitude(transform.position - laneData[currentIndex+1]);
            if (distance2 < 0.1f && !checkReachBlocked)
            {
                ApiManager.Instance?.AddWaypointReached(gameObject, currentIndex);  // currentIndex is right because of +1 waypoint, intial pose.
                if (currentIndex+1 == laneData.Count-1)
                {
                    checkReachBlocked = true;
                    if (laneIdle[currentIndex+1] == -1 && currentDeactivate)
                    {
                        waypointDriveState = WaypointDriveState.Despawn;
                    }
                    else if (laneIdle[currentIndex+1] == 0)
                        waypointDriveState = WaypointDriveState.Wait;
                }
                else if (time > laneTime[currentIndex+1])
                    currentIndex++;

                // Avoid consecutive AddWaypointReached() before time exceeds laneData[currentIndex+1]
                checkReachBlocked = true;
            }
        }
        else if (currentIndex == laneData.Count-1)
        {
            if (laneIdle[currentIndex] == -1 && currentDeactivate)
            {
                waypointDriveState = WaypointDriveState.Despawn;
            }
            else if (laneIdle[currentIndex] == 0)
                waypointDriveState = WaypointDriveState.Wait;

            return;
        }

        (position, rotation) = NPCPoseInterpolate(time, laneData, laneAngle, laneTime, currentIndex);

        if (!float.IsNaN(position.x))
        {
            rb.MovePosition(position);
            rb.MoveRotation(rotation);
        }

        if (currentIndex < laneData.Count-1)
        {
            if (time > laneTime[currentIndex+1])
            {
                currentIndex++;
                checkReachBlocked = false;
            }
        }
    }

    private (Vector3, Quaternion) NPCPoseInterpolate(double time, List<Vector3>poses, List<Vector3>angles, List<float>times, int index)
    {
        // Catmull interpolation needs constrained waypoints input. Zigzag waypoints input makes error for catmull interpolation.
        // Instead, NPCController uses linear interpolation.
        // var pose = CatmullRomInterpolate(time);
        var k = (float)(time - (times[index])) / (times[index+1] - times[index]);
        var pose = Vector3.Lerp(poses[index], poses[index+1], k);
        var rot = Quaternion.Lerp(Quaternion.Euler(angles[index]), Quaternion.Euler(angles[index+1]), k);

        return (pose, rot);
    }

    // As for rotation, Catmull-Rom interpolation doesn't work.
    private Vector3 CatmullRomInterpolate(float time)
    {
        Vector3[] points = new Vector3[4];
        Vector3[] rotations = new Vector3[4];
        float[] times = new float[4];

        Vector3 interpolatedPose = new Vector3();

        var maxIndex = laneTime.Count - 1;

        if (laneData.Count == 2 && laneTime.Count == 2)
        {
            points[1] = laneData[0];
            points[2] = laneData[1];
            points[0] = points[1] + (points[1] - points[2]);
            points[3] = points[2] + (points[2] - points[1]);

            times[1] = laneTime[0];
            times[2] = laneTime[1];
            times[0] = times[1] + (times[1] - times[2]);
            times[3] = times[2] + (times[2] - times[1]);
        }
        else if (laneData.Count == 3 && laneTime.Count == 3)
        {
            if (time >= times[0] && time <= times[1])
            {
                points[1] = laneData[0];
                points[2] = laneData[1];
                points[3] = laneData[2];
                points[0] = points[1] + (points[1] - points[2]);

                times[1] = laneTime[0];
                times[2] = laneTime[1];
                times[3] = laneTime[2];
                times[0] = times[1] + (times[1] - times[2]);
            }

            else if (time >= times[1] && time <= times[2])
            {
                points[0] = laneData[0];
                points[1] = laneData[1];
                points[2] = laneData[2];
                points[3] = points[2] + (points[2] - points[1]);

                times[0] = laneTime[0];
                times[1] = laneTime[1];
                times[2] = laneTime[2];
                times[3] = times[2] + (times[2] - times[1]);
            }
        }
        else if (time <= laneTime[1] && time >= laneTime[0])  // currentIndex == 0, lower bound case
        {
            points[0] = laneData[0] - (laneData[1] - laneData[0]);
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex+1];
            points[3] = laneData[currentIndex+2];

            times[0] = laneTime[0] - (laneTime[1] - laneTime[0]);
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex+1];
            times[3] = laneTime[currentIndex+2];
        }
        else if (time <= laneTime[maxIndex-1] && time > laneTime[1])  // 1 <= currentIndex <= maxIndex-1, most of cases
        {
            points[0] = laneData[currentIndex-1];
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex+1];
            points[3] = laneData[currentIndex+2];

            times[0] = laneTime[currentIndex-1];
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex+1];
            times[3] = laneTime[currentIndex+2];
        }
        else if (laneTime[maxIndex-1] < time)   // maxIndex-1 <= currentIndex, upper bound case
        {
            points[0] = laneData[currentIndex-1];
            points[1] = laneData[currentIndex];
            points[2] = laneData[currentIndex] + (laneData[currentIndex] - laneData[currentIndex-1]);
            points[3] = points[2] + (laneData[currentIndex-1] - laneData[currentIndex-2]);

            times[0] = laneTime[currentIndex-1];
            times[1] = laneTime[currentIndex];
            times[2] = laneTime[currentIndex] + (laneTime[currentIndex] - laneTime[currentIndex-1]);;
            times[3] = times[2] + (laneTime[currentIndex-1] - laneTime[currentIndex-2]);
        }
        else
        {
            Debug.Log($"Couldn't interpolate.");
        }

        interpolatedPose = CatmullRom(points, times, time);

        return interpolatedPose;
    }

    private void NPCProcessIdleTime()
    {
        if (waypointDriveState == WaypointDriveState.Wait)
        {
            currentIdle = laneIdle[0];
            currentDeactivate = laneDeactivate[currentIndex];
            idleCoroutine = FixedUpdateManager.StartCoroutine(IdleNPC(currentIdle, currentDeactivate));
        }
        else if (waypointDriveState == WaypointDriveState.Drive)
        {
            currentIdle = laneIdle[currentIndex];
            currentDeactivate = laneDeactivate[currentIndex];
        }

        if (currentIdle == -1f)
            gameObject.SetActive(false);
    }
    #endregion

    #region targeting
    public void SetLaneData(List<Vector3> data)
    {
        currentIndex = 0;
        laneData = new List<Vector3>(data);

        currentTarget = laneData[++currentIndex];
    }
    #endregion

    public void SetFollowWaypoints(List<DriveWaypoint> waypoints, bool loop)
    {
        waypointLoop = loop;

        laneData = waypoints.Select(wp => wp.Position).ToList();
        laneSpeed = waypoints.Select(wp => wp.Speed).ToList();
        laneAngle = waypoints.Select(wp => wp.Angle).ToList();
        laneIdle = waypoints.Select(wp => wp.Idle).ToList();
        laneDeactivate = waypoints.Select(wp => wp.Deactivate).ToList();
        laneTriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        laneTime = waypoints.Select(wp => wp.TimeStamp).ToList();

        ResetData();

        currentIndex = 0;
        currentTarget = laneData[0];
        currentTargetDirection = (currentTarget - rb.position).normalized;
        currentIdle = laneIdle[0];
        currentDeactivate = laneDeactivate[0];

        if (laneTime[0] < 0)
        {
            // Set waypoint time base on speed.
            Debug.LogWarning("Waypoint timestamps absent or invalid, caluclating timestamps based on speed.");
            laneTime = new List<float>();
            laneTime.Add(0);
            for (int i=0; i < laneData.Count-1; i++)
            {
                var dp = laneData[i+1] - laneData[i];
                var dt = dp.magnitude/laneSpeed[i];
                laneTime.Add(laneTime.Last()+dt);
            }
        }
        updatedWaypoints = true;
        isFirstRun = true;
    }

    private IEnumerator IdleNPC(float duration, bool deactivate)
    {
        currentIdle = 0;
        Vector3 pos = rb.position;
        if (deactivate)
        {
            gameObject.SetActive(false);
        }
        yield return FixedUpdateManager.WaitForFixedSeconds(duration);
        if (deactivate)
        {
            gameObject.SetActive(true);
        }
        wakeUpTime = SimulatorManager.Instance.CurrentTime;
        activateNPC = true;
        waypointDriveState = WaypointDriveState.Drive;

        if (!logWaypoint.ContainsKey(GTID))
            logWaypoint.Add(GTID, new List<string>());
    }

    public static Vector3 CatmullRom(Vector3[] points, float[] times, float t)
    {
        Debug.Assert(points.Length == times.Length, $"points.Length = {points.Length}, times.Length = {times.Length}");
        Debug.Assert(points.Length > 1 || times.Length > 1, $"points.Length = {points.Length}, times.Length = {times.Length}");

        Vector3 A1 = (times[1] - t) / (times[1] - times[0]) * points[0] + (t - times[0]) / (times[1] - times[0]) * points[1];
        Vector3 A2 = (times[2] - t) / (times[2] - times[1]) * points[1] + (t - times[1]) / (times[2] - times[1]) * points[2];
        Vector3 A3 = (times[3] - t) / (times[3] - times[2]) * points[2] + (t - times[2]) / (times[3] - times[2]) * points[3];

        Vector3 B1 = (times[2] - t) / (times[2] - times[0]) * A1 + (t - times[0]) / (times[2] - times[0]) * A2;
        Vector3 B2 = (times[3] - t) / (times[3] - times[1]) * A2 + (t - times[1]) / (times[3] - times[1]) * A3;

        return ((times[2] - t) / (times[2] - times[1]) * B1) + ((t - times[1]) / (times[2] - times[1]) * B2);
    }
    
    public override void OnAgentCollision(GameObject go)
    {
        // TODO
    }
}
