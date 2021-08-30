/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Map;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAutomaticBehaviour : PedestrianBehaviourBase
{
    private Vector3[] Corners = new Vector3[] { };
    private float LinearSpeed = 1.0f;

    public override void PhysicsUpdate()
    {
        switch (controller.MapPath.type)
        {
            case MapAnnotationTool.PedestrianPathType.SIDEWALK:
                EvaluateSidewalk();
                break;
            case MapAnnotationTool.PedestrianPathType.CROSSWALK:
                EvaluateCrosswalk();
                break;
            default:
                break;
        }

        EvaluateDistanceFromFocus();
    }

    public override void SetSpeed(float speed)
    {
        LinearSpeed = speed;
    }

    public override void Init(int seed) { }

    public override void OnAgentCollision(GameObject go) { }

    private void EvaluateSidewalk()
    {
        if (IsRandomIdle())
        {
            controller.Coroutines[(int) PedestrianController.CoroutineID.ChangePedState] =
                FixedUpdateManager.StartCoroutine(controller.ChangePedState());
        }

        if (controller.ThisPedState == PedestrianController.PedestrianState.Walking)
        {
            EvaluateNextTarget();
        }
    }

    private void EvaluateCrosswalk()
    {
        if (!EvaluateOnRoad() && EvaluateOnRoadForward() && !EvaluateSignal())
        {
            controller.SetPedState(PedestrianController.PedestrianState.Idle);
        }
        else
        {
            EvaluateNextTarget();
        }
    }

    private void EvaluateDistanceFromFocus()
    {
        if (SimulatorManager.Instance.IsAPI)
            return;

        if (!SimulatorManager.Instance.PedestrianManager.spawnsManager.WithinSpawnArea(transform.position) &&
            !SimulatorManager.Instance.PedestrianManager.spawnsManager.IsVisible(controller.Bounds))
        {
            SimulatorManager.Instance.PedestrianManager.DespawnPed(controller);
        }
    }

    private bool EvaluateSignal()
    {
        bool go = false;
        foreach (var signal in controller.MapPath.Signals)
        {
            go = signal.CurrentState == "red";
        }

        return go;
    }

    private bool EvaluateOnRoad()
    {
        bool onRoad = false;
        int roadMask = 1 << NavMesh.GetAreaFromName("Road");
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, roadMask))
        {
            onRoad = true;
        }

        return onRoad;
    }

    private bool EvaluateOnRoadForward()
    {
        bool onRoad = false;
        int roadMask = 1 << NavMesh.GetAreaFromName("Road");
        if (NavMesh.SamplePosition(transform.position + transform.forward * 0.5f, out NavMeshHit hit, 1f, roadMask))
        {
            onRoad = true;
        }

        return onRoad;
    }

    private void EvaluateNextTarget()
    {
        if (!controller.IsPathReady())
        {
            controller.GetNextPath();
        }

        Corners = controller.Path.corners;
        Vector3 targetPos = RB.position;
        if (controller.CurrentWP < Corners.Length)
        {
            targetPos = new Vector3(Corners[controller.CurrentWP].x, RB.position.y, Corners[controller.CurrentWP].z);
        }

        Vector3 direction = targetPos - RB.position;

        controller.CurrentTurn = direction;
        controller.MovementSpeed = LinearSpeed;
        controller.SetPedState(PedestrianController.PedestrianState.Walking);

        if (direction.magnitude < controller.Accuracy)
        {
            controller.CurrentWP++;
            if (controller.CurrentWP >= Corners.Length)
            {
                controller.Path.ClearCorners();
                controller.CurrentTargetIndex = controller.NextTargetIndex;
                controller.NextTargetIndex = controller.GetNextTargetIndex(controller.CurrentTargetIndex);
                controller.CurrentWP = 0;
            }
        }
    }

    private bool IsRandomIdle()
    {
        if (RandomGenerator.Next(1000) < 1 && controller.ThisPedState == PedestrianController.PedestrianState.Walking)
        {
            return true;
        }

        return false;
    }
}