/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulator.Api;
using Simulator.Utilities;
using UnityEngine;

public class PedestrianWaypointBehaviour : PedestrianBehaviourBase
{
    #region vars
    private bool DebugMode = false;
    private const float LinearPathRotationSpeed = 180.0f;

    // waypoint data
    public List<float> LaneSpeed;
    public List<float> LaneAcceleration;
    public List<Vector3> LaneData;
    public List<Quaternion> LaneAngle;
    public List<float> LaneIdle;
    public List<float> LaneTime;
    public List<float> LaneTriggerDistance;
    public List<WaypointTrigger> LaneTriggers;
    public bool WaypointLoop;
    public WaypointsPathType PathType;
    public List<Vector3> AccelerationDestination;
    public List<float> AccelerationDuration;


    // targeting
    public Vector3 CurrentTarget;
    public int CurrentIndex = 0;
    public bool CurrentDeactivate = false;
    private int CurrentLoopIndex = 0;

    private Coroutine IdleCoroutine;
    private Coroutine MoveCoroutine;
    private Coroutine TriggerCoroutine;

    private Vector3 InitPos;
    private Quaternion InitRot;

    // Waypoint Walking
    public enum WaypointWalkState
    {
        Trigger,
        Idle,
        Walk,
        Despawn,
    };
    public WaypointWalkState WaypointState = WaypointWalkState.Walk;
    #endregion

    #region mono
    public override void PhysicsUpdate()
    {
        if (WaypointState == WaypointWalkState.Walk)
        {
            if (MoveCoroutine != null)
                return;
            MoveCoroutine = FixedUpdateManager.StartCoroutine(PedestrianMoveIE());
        }
    }

    public override void SetSpeed(float speed)
    {
        LaneSpeed[CurrentIndex] = speed;
    }

    #endregion

    #region override
    public override void Init(int seed)
    {
    }

    public override void InitAPI(PedestrianManager.PedSpawnData data)
    {
        
    }

    public override void OnAgentCollision(GameObject go)
    {
        // TODO
    }

    public override void Reset()
    {
        
    }
    #endregion

    #region init
    private void InitPedestrian()
    {
        Debug.Assert(LaneData != null);
        RB.isKinematic = true;
        // TODO currentSpeed = 0f;
        RB.angularVelocity = Vector3.zero;
        RB.velocity = Vector3.zero;
        CurrentIndex = 0;
        CurrentLoopIndex = 0;
        if (IdleCoroutine != null)
        {
            FixedUpdateManager.StopCoroutine(IdleCoroutine);
            IdleCoroutine = null;
        }
        if (MoveCoroutine != null)
        {
            FixedUpdateManager.StopCoroutine(MoveCoroutine);
            MoveCoroutine = null;
        }
        if (TriggerCoroutine != null)
        {
            FixedUpdateManager.StopCoroutine(TriggerCoroutine);
            TriggerCoroutine = null;
        }
        WaypointState = WaypointWalkState.Walk;
    }

    public void SetFollowWaypoints(List<WalkWaypoint> waypoints, bool loop, WaypointsPathType pathType)
    {
        InitPos = transform.position;
        InitRot = transform.rotation;

        WaypointLoop = loop;
        PathType = pathType;

        // Process waypoints according to the selected waypoint path
        switch (PathType)
        {
            case WaypointsPathType.Linear:
                break;
            case WaypointsPathType.BezierSpline:
                // Add the initial position for Bezier Spline calculations
                var initWaypoint = ((IWaypoint) waypoints[0]).Clone();
                initWaypoint.Position = InitPos;
                waypoints.Insert(0, (WalkWaypoint) initWaypoint);
                var bezier = new BezierSpline<WalkWaypoint>(waypoints.ToArray(), 0.01f);
                waypoints = bezier.GetBezierWaypoints();
                
                // Remove first waypoint as it will be added by another function
                waypoints.RemoveAt(0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(PathType), PathType, null);
        }

        LaneData = waypoints.Select(wp => wp.Position).ToList();
        LaneSpeed = waypoints.Select(wp => wp.Speed).ToList();
        LaneAcceleration = waypoints.Select(wp => wp.Acceleration).ToList();
        LaneAngle = waypoints.Select(wp => Quaternion.Euler(wp.Angle)).ToList();
        LaneIdle = waypoints.Select(wp => wp.Idle).ToList();
        LaneTriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        LaneTriggers = waypoints.Select(wp => wp.Trigger).ToList();
        LaneTime = new List<float>();

        InitPedestrian();
        AddPoseToFirstWaypoint();

        // Calculate acceleration data only if there are no timestamps
        for (int i = 0; i < LaneData.Count - 1; i++)
        {
            var initialPosition = LaneData[i];
            var destination = LaneData[i + 1];
            var initialSpeed = LaneSpeed[i];
            var destinationSpeed = LaneSpeed[i + 1];
            var distance = Vector3.Distance(initialPosition, destination);
            float duration;
            if (LaneAcceleration[i + 1] > 0)
            {
                // If max speed is lower than the initial speed convert acceleration to deceleration
                if (destinationSpeed < initialSpeed)
                    LaneAcceleration[i + 1] *= -1;

                if (!UniformlyAcceleratedMotion.CalculateDuration(LaneAcceleration[i + 1], initialSpeed,
                    distance, ref destinationSpeed, out var accelerationDuration, out var accelerationDistance))
                {
                    // Max speed will not be reached with current acceleration
                    AccelerationDestination.Add(destination);
                    LaneSpeed[i + 1] = destinationSpeed;
                    duration = accelerationDuration;
                    AccelerationDuration.Add(accelerationDuration);
                }
                else
                {
                    // Calculate mixed duration of accelerated and linear movements
                    var accelerationDestination = initialPosition +
                                                  (destination - initialPosition).normalized * accelerationDistance;
                    AccelerationDestination.Add(accelerationDestination);
                    var linearDistance = distance - accelerationDistance;
                    AccelerationDuration.Add(accelerationDuration);
                    duration = accelerationDuration + linearDistance / destinationSpeed;
                }
            }
            else
            {
                // There is no acceleration - apply max speed for uniform linear movement
                AccelerationDuration.Add(0.0f);
                AccelerationDestination.Add(initialPosition);
                duration = distance / destinationSpeed;
            }
            
            // Set waypoint time base on speed.
            LaneTime.Add(LaneTime[i] + duration);
        }
    }

    private void AddPoseToFirstWaypoint()
    {
        LaneData.Insert(0, transform.position);
        LaneAngle.Insert(0, transform.rotation);
        LaneSpeed.Insert(0, 0f);
        LaneAcceleration.Insert(0, 0f);
        LaneIdle.Insert(0, 0f);
        LaneTriggerDistance.Insert(0, 0f);
        LaneTime.Insert(0, 0f);
        LaneTriggers.Insert(0, null);
        AccelerationDestination = new List<Vector3> {LaneData[0]};
        AccelerationDuration = new List<float> {0.0f};
    }

    #endregion

    #region index

    private void EvaluateLane()
    {
        CurrentIndex++; // index can equal laneData.Count so it can finish pedestrian move IE
        if (CurrentIndex < LaneData.Count)
        {
            CurrentTarget = LaneData[CurrentIndex];
            controller.MovementSpeed = LaneSpeed[CurrentIndex];
        }

        if (CurrentIndex == LaneData.Count)
        {
            var api = ApiManager.Instance;
            if (WaypointLoop)
            {
                if (CurrentLoopIndex == 0 && api != null)
                    api.AgentTraversedWaypoints(gameObject);
                CurrentLoopIndex++;
                RB.MovePosition(InitPos);
                RB.MoveRotation(InitRot);
                InitPedestrian();
            }
            else
            {
                if (api != null)
                    api.AgentTraversedWaypoints(gameObject);
                WaypointState = WaypointWalkState.Despawn;
                if (TriggerCoroutine != null)
                    FixedUpdateManager.StopCoroutine(TriggerCoroutine);
                TriggerCoroutine = null;
                if (IdleCoroutine != null)
                    FixedUpdateManager.StopCoroutine(IdleCoroutine);
                IdleCoroutine = null;
                if (MoveCoroutine != null)
                    FixedUpdateManager.StopCoroutine(MoveCoroutine);
                MoveCoroutine = null;
            }
        }
        else
        {
            WaypointState = WaypointWalkState.Walk;
        }
    }
    #endregion

    #region routines
    private IEnumerator PedestrianMoveIE()
    {
        if (CurrentIndex == 0)
        {
            // increment index since spawn is index = 0 with no passed params
            EvaluateLane();
        }

        controller.SetPedState(PedestrianController.PedestrianState.Walking);
        if (CurrentIndex != 0)
        {
            var duration = LaneTime[CurrentIndex] - LaneTime[CurrentIndex - 1];
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                var factor = elapsedTime / duration;
                var pose = Vector3.Lerp(LaneData[CurrentIndex - 1], LaneData[CurrentIndex], factor);
                if (!float.IsNaN(pose.x))
                {
                    var acceleration = LaneAcceleration[CurrentIndex];
                    // Apply uniformly accelerated motion if there is any acceleration or deceleration
                    if (!Mathf.Approximately(acceleration, 0.0f))
                    {
                        var destinationSpeed = LaneSpeed[CurrentIndex];
                        var initialPosition = LaneData[CurrentIndex - 1];
                        var destination = LaneData[CurrentIndex];
                        var initialSpeed = LaneSpeed[CurrentIndex - 1];
                        var accelerationDuration = CurrentIndex < AccelerationDuration.Count
                            ? AccelerationDuration[CurrentIndex]
                            : 0.0f;

                        if (elapsedTime < accelerationDuration)
                        {
                            // Uniformly accelerated movement
                            controller.MovementSpeed = Mathf.Lerp(initialSpeed, destinationSpeed,
                                elapsedTime / accelerationDuration);
                            var distance =
                                UniformlyAcceleratedMotion.CalculateDistance(acceleration, initialSpeed, elapsedTime);
                            var moveTranslation = distance * (destination - initialPosition).normalized;
                            RB.MovePosition(initialPosition + moveTranslation);
                        }
                        else
                        {
                            // Uniform linear movement
                            var t = (elapsedTime - accelerationDuration) / (duration - accelerationDuration);
                            controller.MovementSpeed = destinationSpeed;
                            var accelerationDestination = CurrentIndex < AccelerationDestination.Count
                                ? AccelerationDestination[CurrentIndex]
                                : initialPosition;
                            RB.MovePosition(Vector3.Lerp(accelerationDestination, destination, t));
                        }
                    }
                    else
                    {
                        // Uniform linear movement
                        RB.MovePosition(pose);
                    }

                    // Interpolate the rotation
                    Quaternion rot;
                    switch (PathType)
                    {
                        case WaypointsPathType.Linear:
                            // If it is a linear path just rotate towards with fixed speed
                            var maxRotationDelta = LinearPathRotationSpeed * elapsedTime;
                            rot = Quaternion.RotateTowards(LaneAngle[CurrentIndex - 1], LaneAngle[CurrentIndex],
                                maxRotationDelta);
                            break;

                        case WaypointsPathType.BezierSpline:
                            // For a Bezier path slerp the rotation during the whole duration as it is already preinterpolated during spline generation
                            rot = Quaternion.Slerp(LaneAngle[CurrentIndex - 1], LaneAngle[CurrentIndex], factor);
                            break;
                        default:
                            rot = RB.rotation;
                            break;
                    }

                    RB.MoveRotation(rot);   
                }

                elapsedTime += Mathf.Min(Time.fixedDeltaTime, duration - elapsedTime);
                yield return new WaitForFixedUpdate();
            }

            RB.MovePosition(LaneData[CurrentIndex]);
            RB.MoveRotation(LaneAngle[CurrentIndex]);
        }

        controller.SetPedState(PedestrianController.PedestrianState.Idle);
        if (CurrentIndex <= LaneData.Count - 1)
        {
            //LaneData includes pedestrian position at 0 index, waypoints starts from index 1
            //Because of that index has to be lowered by 1 before passing to the API
            if (ApiManager.Instance != null)
                ApiManager.Instance.AddWaypointReached(gameObject, CurrentIndex - 1);

            // apply simple distance trigger
            if (LaneTriggerDistance[CurrentIndex] > 0)
            {
                WaypointState = WaypointWalkState.Trigger;
                yield return TriggerCoroutine = FixedUpdateManager.StartCoroutine(PedestrianTriggerIE());
            }

            // apply complex triggers
            if (CurrentIndex < LaneTriggers.Count && LaneTriggers[CurrentIndex] != null)
            {
                WaypointState = WaypointWalkState.Trigger;
                yield return TriggerCoroutine =
                    FixedUpdateManager.StartCoroutine(LaneTriggers[CurrentIndex].Apply(controller));
                TriggerCoroutine = null;
            }

            // idle
            if (LaneIdle[CurrentIndex] > 0)
            {
                WaypointState = WaypointWalkState.Idle;
                yield return IdleCoroutine =
                    FixedUpdateManager.StartCoroutine(PedestrianIdleIE(LaneIdle[CurrentIndex], CurrentDeactivate));
            }
            else if (LaneIdle[CurrentIndex] == -1 && CurrentDeactivate)
            {
                WaypointState = WaypointWalkState.Despawn;
                gameObject.SetActive(false);
                MoveCoroutine = null;
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

    private IEnumerator PedestrianIdleIE(float duration, bool deactivate)
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

    private IEnumerator PedestrianTriggerIE()
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