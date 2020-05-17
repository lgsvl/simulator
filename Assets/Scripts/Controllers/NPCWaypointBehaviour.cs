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
    private bool DebugMode = false;

    // waypoint data
    public List<float> LaneSpeed;
    public List<Vector3> LaneData;
    public List<Vector3> LaneAngle;
    public List<float> LaneIdle;
    public List<float> LaneTime;
    public List<bool> LaneDeactivate;
    public List<float> LaneTriggerDistance;
    public bool WaypointLoop;

    // targeting
    public Vector3 CurrentTarget;
    public int CurrentIndex = 0;
    public bool CurrentDeactivate = false;

    private Coroutine IdleCoroutine;
    private Coroutine MoveCoroutine;
    private Coroutine TriggerCoroutine;

    private Vector3 InitPos;
    private Quaternion InitRot;

    // Waypoint Driving
    public enum WaypointDriveState
    {
        Trigger,
        Idle,
        Drive,
        Despawn,
    };
    public WaypointDriveState WaypointState = WaypointDriveState.Drive;
    #endregion

    #region mono
    public override void PhysicsUpdate()
    {
        //controller.SetBrakeLights(currentSpeed < 2.0f); // TODO
        if (WaypointState == WaypointDriveState.Drive)
        {
            if (MoveCoroutine != null)
                return;
            MoveCoroutine = FixedUpdateManager.StartCoroutine(NPCMoveIE());
        }
    }
    #endregion

    #region override
    public override void Init(int seed)
    {
        // api sends init data
    }

    public override void InitLaneData(MapLane lane)
    {
        // Maplane data void in waypoint behavior
    }

    public override void OnAgentCollision(GameObject go)
    {
        // TODO
    }
    #endregion

    #region init
    private void InitNPC()
    {
        Debug.Assert(LaneData != null);
        rb.isKinematic = true;
        controller.MainCollider.isTrigger = true;
        controller.ResetLights();
        currentSpeed = 0f;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        CurrentIndex = 0;
        CurrentDeactivate = LaneDeactivate[CurrentIndex];
        FixedUpdateManager.StopAllCoroutines();
        TriggerCoroutine = null;
        IdleCoroutine = null;
        MoveCoroutine = null;
        WaypointState = WaypointDriveState.Drive;
    }

    public void SetFollowWaypoints(List<DriveWaypoint> waypoints, bool loop)
    {
        InitPos = transform.position;
        InitRot = transform.rotation;

        WaypointLoop = loop;

        LaneData = waypoints.Select(wp => wp.Position).ToList();
        LaneSpeed = waypoints.Select(wp => wp.Speed).ToList();
        LaneAngle = waypoints.Select(wp => wp.Angle).ToList();
        LaneIdle = waypoints.Select(wp => wp.Idle).ToList();
        LaneDeactivate = waypoints.Select(wp => wp.Deactivate).ToList();
        LaneTriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        LaneTime = waypoints.Select(wp => wp.TimeStamp).ToList();

        InitNPC();

        if (LaneTime[0] < 0)
        {
            // Set waypoint time base on speed.
            Debug.LogWarning("Waypoint timestamps absent or invalid, caluclating timestamps based on speed.");
            LaneTime = new List<float>();
            LaneTime.Add(0);
            for (int i = 0; i < LaneData.Count - 1; i++)
            {
                var dp = LaneData[i + 1] - LaneData[i];
                var dt = dp.magnitude / LaneSpeed[i];
                LaneTime.Add(LaneTime.Last() + dt);
            }
        }
        AddPoseToFirstWaypoint();
    }

    private void AddPoseToFirstWaypoint()
    {
        LaneData.Insert(0, transform.position);
        LaneAngle.Insert(0, transform.eulerAngles);
        LaneSpeed.Insert(0, 0f);
        LaneIdle.Insert(0, 0f);
        LaneDeactivate.Insert(0, false);
        LaneTriggerDistance.Insert(0, 0f);
        LaneTime.Insert(0, 0f);

        float initialMoveDuration = (LaneData[1] - LaneData[0]).magnitude / LaneSpeed[1];

        for (int i = 1; i < LaneTime.Count; i++)
        {
            LaneTime[i] += initialMoveDuration;
        }
    }
    #endregion

    #region index
    private void EvaluateLane()
    {
        CurrentIndex++; // index can equal laneData.Count so it can finish npc move IE
        if (CurrentIndex < LaneData.Count)
        {
            CurrentTarget = LaneData[CurrentIndex];
        }

        if (CurrentIndex == LaneData.Count)
        {
            if (WaypointLoop)
            {
                rb.MovePosition(InitPos);
                rb.MoveRotation(InitRot);
                InitNPC();
            }
            else
            {
                WaypointState = WaypointDriveState.Despawn;
                FixedUpdateManager.StopAllCoroutines();
                TriggerCoroutine = null;
                IdleCoroutine = null;
                MoveCoroutine = null;
            }
        }
        else
        {
            WaypointState = WaypointDriveState.Drive;
        }
    }
    #endregion

    #region routines
    private IEnumerator NPCMoveIE()
    {
        if (CurrentIndex == 0)
        {
            // increment index since spawn is index = 0 with no passed params
            EvaluateLane();
        }

        if (CurrentIndex != 0)
        {
            var duration = LaneTime[CurrentIndex] - LaneTime[CurrentIndex - 1];
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                var factor = elapsedTime / duration;
                var pose = Vector3.Lerp(LaneData[CurrentIndex - 1], LaneData[CurrentIndex], factor);
                var rot = Quaternion.Slerp(Quaternion.Euler(LaneAngle[CurrentIndex - 1]), Quaternion.Euler(LaneAngle[CurrentIndex]), factor);
                if (!float.IsNaN(pose.x))
                {
                    rb.MovePosition(pose);
                    rb.MoveRotation(rot);
                }
                elapsedTime += Mathf.Min(Time.fixedDeltaTime, duration - elapsedTime);
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(LaneData[CurrentIndex]);
            rb.MoveRotation(Quaternion.Euler(LaneAngle[CurrentIndex]));
        }

        if (CurrentIndex <= LaneData.Count - 1)
        {
            ApiManager.Instance?.AddWaypointReached(gameObject, CurrentIndex);

            // trigger
            if (LaneTriggerDistance[CurrentIndex] > 0)
            {
                WaypointState = WaypointDriveState.Trigger;
                yield return TriggerCoroutine = FixedUpdateManager.StartCoroutine(NPCTriggerIE());
            }

            // deactivate
            CurrentDeactivate = LaneDeactivate[CurrentIndex];

            // idle
            if (LaneIdle[CurrentIndex] > 0)
            {
                WaypointState = WaypointDriveState.Idle;
                yield return IdleCoroutine = FixedUpdateManager.StartCoroutine(NPCIdleIE(LaneIdle[CurrentIndex], CurrentDeactivate));
            }
            else if (LaneIdle[CurrentIndex] == -1 && CurrentDeactivate)
            {
                WaypointState = WaypointDriveState.Despawn;
                gameObject.SetActive(false);
                yield break;
            }
            else
            {
                // lane
                EvaluateLane();
            }
        }
        MoveCoroutine = null;
    }

    private IEnumerator NPCIdleIE(float duration, bool deactivate)
    {
        if (deactivate)
        {
            gameObject.SetActive(false);
        }
        yield return FixedUpdateManager.WaitForFixedSeconds(duration);
        if (deactivate)
        {
            gameObject.SetActive(true);
        }
        EvaluateLane();
        IdleCoroutine = null;
    }

    private IEnumerator NPCTriggerIE()
    {
        while(SimulatorManager.Instance.AgentManager.GetDistanceToActiveAgent(transform.position) > LaneTriggerDistance[CurrentIndex])
        {
            yield return null;
        }
        TriggerCoroutine = null;
    }
    #endregion

    #region debug
    public void OnDrawGizmos()
    {
        if (!DebugMode)
        {
            return;
        }

        for (int i = 0; i < LaneData.Count - 1; i++)
        {
            Debug.DrawLine(LaneData[i], LaneData[i + 1], Color.red);
        }
        if (LaneData != null && LaneData.Count > 0)
        {
            if (CurrentIndex != 0 && CurrentIndex > LaneData.Count)
            {
                Debug.DrawLine(LaneData[CurrentIndex], LaneData[CurrentIndex - 1], Color.yellow);
            }
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(CurrentTarget, 0.5f);
    }
    #endregion
}
