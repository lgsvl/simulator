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
    private List<float> Speeds;
    private List<float> Idle;
    private List<float> TriggerDistance;
    private List<WaypointTrigger> LaneTriggers;

    private float CurrentTriggerDistance;
    private float CurrentIdle;
    private bool WaypointLoop;
    private WaypointTrigger CurrentTrigger;
    private int CurrentLoopIndex = 0;

    public override void PhysicsUpdate()
    {
        EvaluateWaypointTarget();
    }

    public override void SetSpeed(float speed)
    {
        Speeds[controller.NextTargetIndex] = speed;
    }

    public override void Init(int seed) { }

    public override void OnAgentCollision(GameObject go) { }

    public void FollowWaypoints(List<WalkWaypoint> waypoints, bool loop, WaypointsPathType pathType)
    {
        controller.Reset();

        // Process waypoints according to the selected waypoint path
        switch (pathType)
        {
            case WaypointsPathType.Linear:
                break;
            case WaypointsPathType.BezierSpline:
                var initWaypoint = ((IWaypoint) waypoints[0]).Clone();
                initWaypoint.Position = transform.position;
                waypoints.Insert(0, (WalkWaypoint) initWaypoint);
                var bezier = new BezierSpline<WalkWaypoint>(waypoints.ToArray(), 0.01f);
                waypoints = bezier.GetBezierWaypoints();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
        }

        controller.Agent.avoidancePriority = 0;
        controller.Targets = waypoints.Select(wp => wp.Position).ToList();
        Speeds = waypoints.Select(wp => wp.Speed).ToList();
        Idle = waypoints.Select(wp => wp.Idle).ToList();
        TriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        LaneTriggers = waypoints.Select(wp => wp.Trigger).ToList();
        WaypointLoop = loop;

        CurrentIdle = Idle[0];
        CurrentTriggerDistance = TriggerDistance[0];
        controller.CurrentTargetIndex = 0;
        controller.NextTargetIndex = 0;
        CurrentLoopIndex = 0;
        controller.MovementSpeed = Speeds[controller.NextTargetIndex];

        controller.SetPedState(PedestrianController.PedestrianState.Walking);
    }

    private void EvaluateWaypointTarget()
    {
        if (controller.ThisPedState != PedestrianController.PedestrianState.Walking)
        {
            return;
        }

        if (!controller.IsPathReady())
        {
            controller.GetNextPath();
        }

        var corners = controller.Path.corners;
        Vector3 targetPos = RB.position;
        if (controller.CurrentWP < corners.Length)
        {
            targetPos = new Vector3(corners[controller.CurrentWP].x, RB.position.y, corners[controller.CurrentWP].z);
        }

        Vector3 direction = targetPos - RB.position;

        controller.CurrentTurn = direction;
        controller.MovementSpeed = Speeds[controller.NextTargetIndex];

        if (!(direction.magnitude < controller.Accuracy))
        {
            return;
        }

        controller.CurrentWP++;
        if (controller.CurrentWP >= corners.Length)
        {
            var api = ApiManager.Instance;
            if (api != null
            ) // When waypoint is reached, Ped waits for trigger (if any), then idles (if any), then moves on to next waypoint
            {
                api.AddWaypointReached(gameObject, controller.NextTargetIndex);
            }

            controller.Path.ClearCorners();
            CurrentIdle = Idle[controller.NextTargetIndex];
            CurrentTriggerDistance = TriggerDistance[controller.NextTargetIndex];
            CurrentTrigger = LaneTriggers.Count > controller.NextTargetIndex
                ? LaneTriggers[controller.NextTargetIndex]
                : null;
            controller.CurrentTargetIndex = controller.NextTargetIndex;
            controller.NextTargetIndex = controller.GetNextTargetIndex(controller.CurrentTargetIndex);
            controller.CurrentWP = 0;

            if (CurrentTriggerDistance > 0f)
            {
                controller.SetPedState(PedestrianController.PedestrianState.Idle);
                controller.Coroutines[(int) PedestrianController.CoroutineID.WaitForAgent] =
                    FixedUpdateManager.StartCoroutine(EvaluateEgoToTrigger(controller.NextTargetPos,
                        CurrentTriggerDistance));
            }
            else if (CurrentTrigger != null) // apply complex triggers
            {
                var previousState = controller.ThisPedState;
                var callback = new Action(() => { controller.ThisPedState = previousState; });
                controller.Coroutines[(int) PedestrianController.CoroutineID.WaitForAgent] =
                    FixedUpdateManager.StartCoroutine(CurrentTrigger.Apply(controller, callback));
                controller.SetPedState(PedestrianController.PedestrianState.Idle);
            }
            else if (controller.ThisPedState == PedestrianController.PedestrianState.Walking && CurrentIdle > 0f)
            {
                controller.Coroutines[(int) PedestrianController.CoroutineID.IdleAnimation] =
                    FixedUpdateManager.StartCoroutine(controller.IdleAnimation(CurrentIdle));
            }

            if (controller.CurrentTargetIndex == controller.Targets.Count - 1)
            {
                if (!WaypointLoop)
                {
                    if (api != null)
                    {
                        api.AgentTraversedWaypoints(gameObject);
                    }

                    controller.WalkRandomly(false);
                }
                else
                {
                    if (CurrentLoopIndex == 0 && api != null)
                    {
                        api.AgentTraversedWaypoints(gameObject);
                    }

                    CurrentLoopIndex++;
                }
            }
        }
    }

    private IEnumerator EvaluateEgoToTrigger(Vector3 pos, float dist)
    {
        // for ego in list of egos
        var players = SimulatorManager.Instance.AgentManager.ActiveAgents;
        while (true)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (Vector3.Distance(players[i].AgentGO.transform.position, pos) < dist)
                {
                    CurrentTriggerDistance = 0;
                    controller.SetPedState(PedestrianController.PedestrianState.Walking);
                    yield break;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}