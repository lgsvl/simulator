/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulator;
using UnityEngine;
using UnityEngine.AI;
using Simulator.Api;
using Simulator.Map;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Identification;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Utilities;

public enum PedestrianState
{
    None,
    Idle,
    Walking,
    Crossing
};

public class PedestrianController : DistributedComponent, IGloballyUniquelyIdentified
{
    public enum ControlType
    {
        Automatic,
        Waypoints,
        Manual,
    }

    [HideInInspector]
    public ControlType Control = ControlType.Automatic;

    List<Vector3> targets;
    List<float> idle;
    List<float> triggerDistance;
    private float CurrentTriggerDistance;
    private float CurrentIdle;
    bool waypointLoop;

    private int CurrentTargetIndex = 0;
    private int NextTargetIndex = 0;
    public float idleTime = 0f;
    public float targetRange = 1f;

    private Vector3 NextTargetPos;
    private Transform NextTargetT;
    private NavMeshAgent agent;
    private Animator anim;
    private PedestrianState thisPedState = PedestrianState.None;
    private System.Random RandomGenerator;
    private MonoBehaviour FixedUpdateManager;
    [HideInInspector]
    public string Name;
    private NavMeshPath Path;
    private int CurrentWP = 0;
    private float LinearSpeed = 1.0f;
    private float AngularSpeed = 10.0f;
    private float Accuracy = 0.5f;
    public Rigidbody rb;
    private Coroutine[] Coroutines = new Coroutine[System.Enum.GetNames(typeof(CoroutineID)).Length];
    private Vector3 CurrentTurn;
    private float CurrentSpeed;
    public Vector3 CurrentVelocity;
    public Vector3 CurrentAngularVelocity;
    private Vector3 LastRBPosition;
    private Quaternion LastRBRotation;
    public uint GTID { get; set; }
    public string GUID { get; set; }
    public Bounds Bounds;

    public PedestrianState ThisPedState
    {
        get => thisPedState;
        set
        {
            if (thisPedState == value)
                return;
            thisPedState = value;
            if (Loader.Instance.Network.IsMaster)
                BroadcastSnapshot();
        }
    }

    private enum CoroutineID
    {
        ChangePedState = 0,
        IdleAnimation = 1,
        WaitForAgent = 2,
    }

    #region api
    public void WalkRandomly(bool enable)
    {
        Reset();

        if (!enable)
        {
            Control = ControlType.Manual;
            ThisPedState = PedestrianState.None;
            return;
        }

        agent.avoidancePriority = RandomGenerator.Next(1, 100);

        var position = agent.transform.position;
        MapPedestrian closest = null;
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
        targets = closest.mapWorldPositions;

        NextTargetIndex = closestIndex;
        Control = ControlType.Automatic;
    }

    public void FollowWaypoints(List<WalkWaypoint> waypoints, bool loop)
    {
        Reset();

        agent.avoidancePriority = 0;

        targets = waypoints.Select(wp => wp.Position).ToList();
        idle = waypoints.Select(wp => wp.Idle).ToList();
        triggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();

        CurrentIdle = idle[0];
        CurrentTriggerDistance = triggerDistance[0];
        CurrentTargetIndex = 0;
        NextTargetIndex = 0;

        Control = ControlType.Waypoints;
        waypointLoop = loop;
    }

    public void InitManual(PedestrianManager.PedSpawnData data)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(data.Seed);
        Path = new NavMeshPath();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        Name = transform.GetChild(0).name;

        agent.avoidancePriority = 0;

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.Warp(data.Position);
        agent.transform.rotation = data.Rotation;

        ThisPedState = PedestrianState.None;
        Control = ControlType.Manual;
    }
    #endregion

    public void InitPed(Vector3 position, List<Vector3> pedSpawnerTargets, int seed)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(seed);
        Path = new NavMeshPath();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        targets = pedSpawnerTargets;
        Name = transform.GetChild(0).name;

        if (RandomGenerator.Next(2) == 0)
            targets.Reverse();

        agent.avoidancePriority = RandomGenerator.Next(1, 100); // set to 0 for no avoidance

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.Warp(position);
        agent.transform.rotation = Quaternion.identity;
    }

    public void SetGroundTruthBox()
    {
        var capsule = GetComponent<CapsuleCollider>();
        Bounds = new Bounds(transform.position, Vector3.zero);
        Bounds.size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, Bounds.size.y / 2, gtBoxCollider.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");
    }

    private bool IsPathReady()
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

    public void PhysicsUpdate()
    {
        if (Control == ControlType.Automatic)
        {
            if (IsRandomIdle())
            {
                Coroutines[(int)CoroutineID.ChangePedState] = FixedUpdateManager.StartCoroutine(ChangePedState());
            }

            if (ThisPedState == PedestrianState.Idle)
            {
                CurrentTurn = Vector3.zero;
                CurrentSpeed = 0f;
            }
            else
            {
                if (!IsPathReady())
                {
                    NextTargetPos = GetRandomTargetPosition(NextTargetIndex);
                    agent.enabled = true;
                    agent.CalculatePath(NextTargetPos, Path);
                    agent.enabled = false;
                }

                var corners = Path.corners;
                Vector3 targetPos = rb.position;
                if (CurrentWP < corners.Length)
                {
                    targetPos = new Vector3(corners[CurrentWP].x, rb.position.y, corners[CurrentWP].z);
                }
                Vector3 direction = targetPos - rb.position;

                CurrentTurn = direction;
                CurrentSpeed = LinearSpeed;
                ThisPedState = PedestrianState.Walking;

                if (direction.magnitude < Accuracy)
                {
                    CurrentWP++;
                    if (CurrentWP >= corners.Length)
                    {
                        Path.ClearCorners();
                        CurrentTargetIndex = NextTargetIndex;
                        NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
                        CurrentWP = 0;
                    }
                }
            }
            EvaluateDistanceFromFocus();
        }
        else if (Control == ControlType.Waypoints)
        {
            EvaluateWaypointTarget();
        }
        else if (Control == ControlType.Manual)
        {
            CurrentTurn = Vector3.zero;
            CurrentSpeed = 0f;
        }

        PEDTurn();
        PEDMove();
        SetAnimationControllerParameters();
    }

    private void EvaluateWaypointTarget()
    {
        if (ThisPedState == PedestrianState.Idle)
        {
            CurrentTurn = Vector3.zero;
            CurrentSpeed = 0f;
        }
        else
        {
            if (!IsPathReady())
            {
                NextTargetPos = targets[NextTargetIndex];
                agent.enabled = true;
                agent.CalculatePath(NextTargetPos, Path);
                agent.enabled = false;
            }

            var corners = Path.corners;
            Vector3 targetPos = rb.position;
            if (CurrentWP < corners.Length)
            {
                targetPos = new Vector3(corners[CurrentWP].x, rb.position.y, corners[CurrentWP].z);
            }
            Vector3 direction = targetPos - rb.position;

            CurrentTurn = direction;
            CurrentSpeed = LinearSpeed;
            ThisPedState = PedestrianState.Walking;

            if (direction.magnitude < Accuracy)
            {
                CurrentWP++;
                if (CurrentWP >= corners.Length)
                {
                    // When waypoint is reached, Ped waits for trigger (if any), then idles (if any), then moves on to next waypoint
                    ApiManager.Instance?.AddWaypointReached(gameObject, NextTargetIndex);

                    Path.ClearCorners();
                    CurrentIdle = idle[NextTargetIndex];
                    CurrentTriggerDistance = triggerDistance[NextTargetIndex];
                    CurrentTargetIndex = NextTargetIndex;
                    NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
                    CurrentWP = 0;

                    if (CurrentTriggerDistance > 0f)
                    {
                        ThisPedState = PedestrianState.Idle;
                        Coroutines[(int)CoroutineID.WaitForAgent] = FixedUpdateManager.StartCoroutine(EvaluateEgoToTrigger(NextTargetPos, CurrentTriggerDistance));
                    }
                    else if (ThisPedState == PedestrianState.Walking && CurrentIdle > 0f)
                    {
                        Coroutines[(int)CoroutineID.IdleAnimation] = FixedUpdateManager.StartCoroutine(IdleAnimation(CurrentIdle));
                    }

                    if (CurrentTargetIndex == targets.Count - 1 && !waypointLoop)
                    {
                        WalkRandomly(false);
                    }
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
                    ThisPedState = PedestrianState.Walking;
                    yield break;
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private void Debug(int i=1)
    {
        int frame;
        if (SimulatorManager.Instance.IsAPI)
            frame = ((ApiManager)FixedUpdateManager).CurrentFrame;
        else
            frame = ((SimulatorManager)FixedUpdateManager).CurrentFrame;

        if (frame % i == 0)
        {
            print(frame + ": " + Name.Substring(0, Name.IndexOf("(")) + " " + transform.position.ToString("F7") + " " + CurrentSpeed + " " + CurrentTurn.ToString("F7"));
        }
    }

    private void PEDTurn()
    {
        if (CurrentTurn != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(CurrentTurn), AngularSpeed * Time.fixedDeltaTime));
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }

        var euler1 = LastRBRotation.eulerAngles;
        var euler2 = rb.rotation.eulerAngles;
        var diff = euler2 - euler1;
        for (int i = 0; i < 3; i++)
        {
            diff[i] = (diff[i] + 180) % 360 - 180;
        }
        CurrentAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
    }

    private void PEDMove()
    {
        if (CurrentSpeed != 0f)
        {
            rb.MovePosition(rb.position + transform.forward * CurrentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }

        CurrentVelocity = (rb.position - LastRBPosition) / Time.fixedDeltaTime;
        LastRBPosition = rb.position;
    }

    private IEnumerator ChangePedState()
    {
        return IdleAnimation(RandomGenerator.NextFloat(idleTime * 0.5f, idleTime));
    }

    private IEnumerator IdleAnimation(float duration)
    {
        if (agent == null || duration == 0f)
        {
            yield break;
        }

        ThisPedState = PedestrianState.Idle;

        yield return FixedUpdateManager.WaitForFixedSeconds(duration);

        ThisPedState = PedestrianState.Walking;
    }

    private bool IsRandomIdle()
    {
        if (RandomGenerator.Next(1000) < 1 && ThisPedState == PedestrianState.Walking)
        {
            return true;
        }
        return false;
    }

    private void SetAnimationControllerParameters()
    {
        if (agent == null || anim == null) return;

        if (ThisPedState == PedestrianState.Walking || ThisPedState == PedestrianState.Crossing) 
            anim.SetFloat("speed", LinearSpeed);
        else 
            anim.SetFloat("speed", 0.0f);
    }

    private int GetNextTargetIndex(int index)
    {
        return index >= targets.Count - 1 ? 0 : index + 1;
    }

    private Vector3 GetRandomTargetPosition(int index)
    {
        Vector3 tempV = Vector3.zero;
        if (targets.Count > 0 && index >= 0 && index < targets.Count)
        {
            tempV = targets[index];
        }

        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh && count < 10000)
        {
            Vector3 randomPoint = tempV + RandomGenerator.InsideUnitSphere() * targetRange;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                tempV = hit.position;
                isInNavMesh = true;
            }
            count++;
        }

        return tempV;
    }

    public void StopPEDCoroutines()
    {
        foreach (Coroutine coroutine in Coroutines)
        {
            if (coroutine != null)
            {
                FixedUpdateManager.StopCoroutine(coroutine);
            }
        }
    }

    private void Reset()
    {
        StopPEDCoroutines();
        Path.ClearCorners();
        CurrentWP = 0;
        ThisPedState = PedestrianState.None;
    }

    private void EvaluateDistanceFromFocus()
    {
        if (SimulatorManager.Instance.IsAPI)
            return;

        if (!SimulatorManager.Instance.PedestrianManager.WithinSpawnArea(transform.position) && !SimulatorManager.Instance.PedestrianManager.IsVisible(gameObject))
        {
            SimulatorManager.Instance.PedestrianManager.DespawnPed(this);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Agent"))
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementPedCollision();
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
        }
    }

    #region network
    protected override string ComponentKey { get; } = "PedestrianController";

    protected override void PushSnapshot(BytesStack messageContent)
    {
        messageContent.PushEnum<PedestrianState>((int)ThisPedState);
    }

    protected override void ApplySnapshot(DistributedMessage distributedMessage)
    {
        ThisPedState = distributedMessage.Content.PopEnum<PedestrianState>();
        //Validate animator, as snapshot can be received before it is initialized
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController!=null)
            SetAnimationControllerParameters();
    }
    #endregion
}
