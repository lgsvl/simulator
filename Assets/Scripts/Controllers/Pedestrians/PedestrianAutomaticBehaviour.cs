/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAutomaticBehaviour : PedestrianBehaviourBase
{
    public NavMeshObstacle NavMeshObstacle { get; set; }
    public NavMeshAgent Agent { get; set; }
    public NavMeshPath Path { get; set; }
    public Vector3 NextTargetPos { get; set; }
    private Vector3[] Corners = new Vector3[] { };
    private float LinearSpeed = 1.0f;
    private float AngularSpeed = 10.0f;
    private Vector3 LastRBPosition;
    private Quaternion LastRBRotation;

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
        PEDTurn();
        PEDMove();
    }

    public override void SetSpeed(float speed)
    {
        LinearSpeed = speed;
    }

    public override void Init(int seed)
    {
        Path = new NavMeshPath();
        Agent = GetComponent<NavMeshAgent>();
        NavMeshObstacle = GetComponent<NavMeshObstacle>();
        Agent.avoidancePriority = RandomGenerator.Next(1, 100); // set to 0 for no avoidance
        Agent.updatePosition = false;
        Agent.updateRotation = false;
        Agent.Warp(RB.position);
        Agent.transform.rotation = Quaternion.identity;
    }

    public override void InitAPI(PedestrianManager.PedSpawnData data)
    {
        Path = new NavMeshPath();
        Agent = GetComponent<NavMeshAgent>();
        Agent.avoidancePriority = 0;
        Agent.updatePosition = false;
        Agent.updateRotation = false;
        Agent.Warp(RB.position);
        Agent.transform.rotation = RB.rotation;
        GetComponent<NavMeshObstacle>().enabled = false;
    }

    public override void OnAgentCollision(GameObject go) { }

    public override void Reset()
    {
        Path.ClearCorners();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((LayerMask.GetMask("Agent", "NPC", "Pedestrian", "Obstacle") & 1 << collision.gameObject.layer) != 0)
        {
            GetNextPath(false);
        }
    }

    public void WalkRandomly()
    {
        Agent.avoidancePriority = RandomGenerator.Next(1, 100);

        var position = Agent.transform.position;
        MapPedestrianLane closest = null;
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        foreach (var path in SimulatorManager.Instance.MapManager.pedestrianLanes)
        {
            for (int i = 0; i < path.mapWorldPositions.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(position - path.mapWorldPositions[i]);
                if (distance < closestDistance)
                {
                    closest = path;
                    closestIndex = i;
                    closestDistance = distance;
                }
            }
        }

        if (closest != null)
        {
            controller.Targets = closest.mapWorldPositions;
            controller.MapPath = closest;
            controller.NextTargetIndex = closestIndex;
        }
        else
        {
            controller.SetBehaviour<PedestrianBehaviourBase>();
            controller.SetPedState(PedestrianController.PedestrianState.Idle);
            Debug.LogError("No pedestrian annotation found, please create annotation, setting to idle");
        }
    }

    private void PEDTurn()
    {
        if (controller.CurrentTurn != Vector3.zero)
        {
            RB.MoveRotation(Quaternion.Slerp(RB.rotation, Quaternion.LookRotation(controller.CurrentTurn), AngularSpeed * Time.fixedDeltaTime));
        }
        else
        {
            RB.angularVelocity = Vector3.zero;
        }
    
        var euler1 = LastRBRotation.eulerAngles;
        var euler2 = RB.rotation.eulerAngles;
        var diff = euler2 - euler1;
        for (int i = 0; i < 3; i++)
        {
            diff[i] = (diff[i] + 180) % 360 - 180;
        }
        controller.CurrentAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
    }
    
    private void PEDMove()
    {
        if (controller.MovementSpeed != 0f)
        {
            RB.MovePosition(RB.position + transform.forward * (controller.MovementSpeed * Time.fixedDeltaTime));
        }
        else
        {
            RB.velocity = Vector3.zero;
        }
    
        var previousVelocity = controller.CurrentVelocity;
        controller.CurrentVelocity = (RB.position - LastRBPosition) / Time.fixedDeltaTime;
        controller.CurrentAcceleration = controller.CurrentVelocity - previousVelocity;
        LastRBPosition = RB.position;
    }

    public bool IsPathReady()
    {
        if (Path.corners.Length == 0 || Path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    
    public void GetNextPath(bool GetNextTarget = true)
    {
        if (GetNextTarget)
        {
            NextTargetPos = GetRandomTargetPosition(controller.NextTargetIndex);
        }
        if (NavMeshObstacle != null)
        {
            NavMeshObstacle.enabled = false;
        }
        Agent.enabled = true;
        Agent.CalculatePath(NextTargetPos, Path);
        Agent.enabled = false;
        if (NavMeshObstacle != null)
        {
            NavMeshObstacle.enabled = true;
        }
    }

    private Vector3 GetRandomTargetPosition(int index)
    {
        Vector3 tempV = Vector3.zero;
        if (controller.Targets.Count > 0 && index >= 0 && index < controller.Targets.Count)
        {
            tempV = controller.Targets[index];
        }

        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh && count < 10000)
        {
            Vector3 randomPoint = tempV + RandomGenerator.InsideUnitSphere() * controller.TargetRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                tempV = hit.position;
                isInNavMesh = true;
                count = 10000;
            }

            count++;
        }

        return tempV;
    }

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
        if (!IsPathReady())
        {
            GetNextPath();
        }

        Corners = Path.corners;
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
                Path.ClearCorners();
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