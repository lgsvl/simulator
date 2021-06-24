/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
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

public class PedestrianController : DistributedComponent, ITriggerAgent, IGloballyUniquelyIdentified
{
    public enum PedestrianState
    {
        Idle,
        Walking,
    };

    public enum ControlType
    {
        Automatic,
        Waypoints,
        None,
    }

    [HideInInspector]
    public ControlType Control = ControlType.Automatic;
    [HideInInspector]
    public string Name;
    [HideInInspector]
    public Rigidbody RB;
    [HideInInspector]
    public Vector3 CurrentVelocity;
    [HideInInspector]
    public Vector3 CurrentAcceleration;
    [HideInInspector]
    public Vector3 CurrentAngularVelocity;

    private List<Vector3> Targets;
    private List<float> Speeds;
    private List<float> Idle;
    private List<float> TriggerDistance;
    private List<WaypointTrigger> LaneTriggers;
    private float CurrentTriggerDistance;
    private WaypointTrigger CurrentTrigger;
    private float CurrentIdle;
    private bool WaypointLoop;

    private int CurrentTargetIndex = 0;
    private int NextTargetIndex = 0;
    private int CurrentLoopIndex = 0;
    private float IdleTime = 3f;
    private float TargetRange = 1f;

    private Vector3 NextTargetPos;
    private NavMeshAgent Agent;
    private Animator Anim;
    private System.Random RandomGenerator;
    private MonoBehaviour FixedUpdateManager;
    private NavMeshPath Path;
    private MapPedestrianLane MapPath;
    private int CurrentWP = 0;
    private float LinearSpeed = 1.0f;
    private float AngularSpeed = 10.0f;
    private float Accuracy = 0.5f;
    private Coroutine[] Coroutines = new Coroutine[System.Enum.GetNames(typeof(CoroutineID)).Length];
    private Vector3 CurrentTurn;
    private float CurrentSpeed;
    private Vector3 LastRBPosition;
    private Quaternion LastRBRotation;
    private Vector3[] Corners = new Vector3[] { };
    private NavMeshObstacle NavMeshObstacle;

    public Transform AgentTransform => transform;
    public Bounds Bounds { get; private set; }
    public Vector3 Acceleration => CurrentAcceleration;
    public uint GTID { get; set; }
    public string GUID { get; set; }

    public float MovementSpeed
    {
        get => CurrentSpeed;
        private set
        {
            if (Mathf.Approximately(CurrentSpeed, value))
                return;
            CurrentSpeed = value;
            if (Loader.Instance.Network.IsMaster)
                BroadcastSnapshot(true);
            if (!Loader.Instance.Network.IsClient)
                SetAnimationControllerParameters();
        }
    }

    public PedestrianState ThisPedState
    {
        get => thisPedState;
        set
        {
            if (thisPedState == value)
                return;

            thisPedState = value;
            if (Loader.Instance.Network.IsMaster)
                BroadcastSnapshot(true);
            if (!Loader.Instance.Network.IsClient)
                SetAnimationControllerParameters();
        }
    }
    private PedestrianState thisPedState = PedestrianState.Idle;

    private enum CoroutineID
    {
        ChangePedState = 0,
        IdleAnimation = 1,
        WaitForAgent = 2,
    }

    public void PhysicsUpdate()
    {
        if (!this.enabled)
            return;

        switch (Control)
        {
            case ControlType.Automatic:
                switch (MapPath.type)
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
                break;
            case ControlType.Waypoints:
                EvaluateWaypointTarget();
                break;
            case ControlType.None:
                break;
            default:
                break;
        }

        PEDTurn();
        PEDMove();
    }

    private void SetPedState(PedestrianState state)
    {
        ThisPedState = state;
        switch (state)
        {
            case PedestrianState.Idle:
                CurrentTurn = Vector3.zero;
                MovementSpeed = 0f;
                break;
            case PedestrianState.Walking:
                break;
            default:
                break;
        }
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

    private void GetNextPath(bool GetNextTarget = true)
    {
        if (GetNextTarget)
        {
            NextTargetPos = GetRandomTargetPosition(NextTargetIndex);
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

    public void SetGroundTruthBox()
    {
        var capsule = GetComponent<CapsuleCollider>();
        var renderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (renderer == null) // create bounds from mesh or use default
        {
            Bounds = new Bounds(transform.position, Vector3.zero)
            {
                size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2)
            };
        }
        else
        {
            Bounds = renderer.bounds;
        }

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, Bounds.size.y / 2, gtBoxCollider.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");
    }

    #region random
    public void InitPed(Vector3 position, int spawnIndex, List<Vector3> pedSpawnerTargets, int seed, MapPedestrianLane mapPath = null)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(seed);
        Path = new NavMeshPath();
        MapPath = mapPath;
        Reset();

        Agent = GetComponent<NavMeshAgent>();
        NavMeshObstacle = GetComponent<NavMeshObstacle>();
        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        Targets = pedSpawnerTargets;
        Name = transform.GetChild(0).name;
        GTID = ++SimulatorManager.Instance.GTIDs;

        CurrentTargetIndex = NextTargetIndex = spawnIndex; // TODO ++Nextindex

        if (RandomGenerator.Next(2) == 0)
        {
            Targets.Reverse();
        }

        Agent.avoidancePriority = RandomGenerator.Next(1, 100); // set to 0 for no avoidance
        Agent.updatePosition = false;
        Agent.updateRotation = false;
        Agent.Warp(position);
        Agent.transform.rotation = Quaternion.identity;

        SetPedState(PedestrianState.Walking);
    }

    private void EvaluateSidewalk()
    {
        if (IsRandomIdle())
        {
            Coroutines[(int)CoroutineID.ChangePedState] = FixedUpdateManager.StartCoroutine(ChangePedState());
        }
        if (ThisPedState == PedestrianState.Walking)
        {
            EvaluateNextTarget();
        }
    }

    private void EvaluateCrosswalk()
    {
        if (!EvaluateOnRoad() && EvaluateOnRoadForward() && !EvaluateSignal())
        {
            SetPedState(PedestrianState.Idle);
        }
        else
        {
            EvaluateNextTarget();
        }
    }

    private bool EvaluateSignal()
    {
        bool go = false;
        foreach (var signal in MapPath.Signals)
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
        if (CurrentWP < Corners.Length)
        {
            targetPos = new Vector3(Corners[CurrentWP].x, RB.position.y, Corners[CurrentWP].z);
        }
        Vector3 direction = targetPos - RB.position;

        CurrentTurn = direction;
        MovementSpeed = LinearSpeed;
        SetPedState(PedestrianState.Walking);

        if (direction.magnitude < Accuracy)
        {
            CurrentWP++;
            if (CurrentWP >= Corners.Length)
            {
                Path.ClearCorners();
                CurrentTargetIndex = NextTargetIndex;
                NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
                CurrentWP = 0;
            }
        }
    }
    #endregion

    #region api
    public void InitAPIPed(PedestrianManager.PedSpawnData data)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(data.Seed);
        Path = new NavMeshPath();
        Reset();

        Agent = GetComponent<NavMeshAgent>();
        GetComponent<NavMeshObstacle>().enabled = false;
        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        RB.isKinematic = false;
        Name = transform.GetChild(0).name;
        GTID = ++SimulatorManager.Instance.GTIDs;

        Agent.avoidancePriority = 0;
        Agent.updatePosition = false;
        Agent.updateRotation = false;
        Agent.Warp(data.Position);
        Agent.transform.rotation = data.Rotation;

        SetPedState(PedestrianState.Idle);
        Control = ControlType.None;
    }

    public void WalkRandomly(bool enable)
    {
        Reset();

        if (!enable)
        {
            Control = ControlType.None;
            SetPedState(PedestrianState.Idle);
            return;
        }

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
        Targets = closest.mapWorldPositions;
        MapPath = closest;

        NextTargetIndex = closestIndex;
        Control = ControlType.Automatic;
        SetPedState(PedestrianState.Walking);
    }

    public void FollowWaypoints(List<WalkWaypoint> waypoints, bool loop)
    {
        Reset();

        Agent.avoidancePriority = 0;
        Targets = waypoints.Select(wp => wp.Position).ToList();
        Speeds = waypoints.Select(wp => wp.Speed).ToList();
        Idle = waypoints.Select(wp => wp.Idle).ToList();
        TriggerDistance = waypoints.Select(wp => wp.TriggerDistance).ToList();
        LaneTriggers = waypoints.Select(wp => wp.Trigger).ToList();
        WaypointLoop = loop;

        CurrentIdle = Idle[0];
        CurrentTriggerDistance = TriggerDistance[0];
        CurrentTargetIndex = 0;
        NextTargetIndex = 0;
        CurrentLoopIndex = 0;
        MovementSpeed = Speeds[NextTargetIndex];

        Control = ControlType.Waypoints;
        SetPedState(PedestrianState.Walking);
    }

    public void SetSpeed(float speed)
    {
        switch (Control)
        {
            case ControlType.Automatic:
                LinearSpeed = speed;
                break;
            case ControlType.Waypoints:
                Speeds[NextTargetIndex] = speed;
                break;
            case ControlType.None:
                break;
        }
    }

    private void EvaluateWaypointTarget()
    {
        if (ThisPedState == PedestrianState.Walking)
        {
            if (!IsPathReady())
            {
                GetNextPath();
            }

            var corners = Path.corners;
            Vector3 targetPos = RB.position;
            if (CurrentWP < corners.Length)
            {
                targetPos = new Vector3(corners[CurrentWP].x, RB.position.y, corners[CurrentWP].z);
            }
            Vector3 direction = targetPos - RB.position;

            CurrentTurn = direction;
            MovementSpeed = Speeds[NextTargetIndex];

            if (direction.magnitude < Accuracy)
            {
                CurrentWP++;
                if (CurrentWP >= corners.Length)
                {
                    var api = ApiManager.Instance;
                    if (api != null) // When waypoint is reached, Ped waits for trigger (if any), then idles (if any), then moves on to next waypoint
                    {
                        api.AddWaypointReached(gameObject, NextTargetIndex);
                    }

                    Path.ClearCorners();
                    CurrentIdle = Idle[NextTargetIndex];
                    CurrentTriggerDistance = TriggerDistance[NextTargetIndex];
                    CurrentTrigger = LaneTriggers.Count > NextTargetIndex ? LaneTriggers[NextTargetIndex] : null;
                    CurrentTargetIndex = NextTargetIndex;
                    NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
                    CurrentWP = 0;

                    if (CurrentTriggerDistance > 0f)
                    {
                        SetPedState(PedestrianState.Idle);
                        Coroutines[(int)CoroutineID.WaitForAgent] = FixedUpdateManager.StartCoroutine(EvaluateEgoToTrigger(NextTargetPos, CurrentTriggerDistance));
                    }
                    else if (CurrentTrigger != null) // apply complex triggers
                    {
                        var previousState = thisPedState;
                        var callback = new Action(() =>
                        {
                            ThisPedState = previousState;
                        });
                        SetPedState(PedestrianState.Idle);
                        Coroutines[(int)CoroutineID.WaitForAgent] = FixedUpdateManager.StartCoroutine(CurrentTrigger.Apply(this, callback));
                    }
                    else if (ThisPedState == PedestrianState.Walking && CurrentIdle > 0f)
                    {
                        Coroutines[(int)CoroutineID.IdleAnimation] = FixedUpdateManager.StartCoroutine(IdleAnimation(CurrentIdle));
                    }

                    if (CurrentTargetIndex == Targets.Count - 1)
                    {
                        if (!WaypointLoop)
                        {
                            if (api != null)
                            {
                                api.AgentTraversedWaypoints(gameObject);
                            }
                            WalkRandomly(false);
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
                    SetPedState(PedestrianState.Walking);
                    yield break;
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }
    #endregion

    private void PEDTurn()
    {
        if (CurrentTurn != Vector3.zero)
        {
            RB.MoveRotation(Quaternion.Slerp(RB.rotation, Quaternion.LookRotation(CurrentTurn), AngularSpeed * Time.fixedDeltaTime));
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
        CurrentAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
    }

    private void PEDMove()
    {
        if (MovementSpeed != 0f)
        {
            RB.MovePosition(RB.position + transform.forward * (MovementSpeed * Time.fixedDeltaTime));
        }
        else
        {
            RB.velocity = Vector3.zero;
        }

        var previousVelocity = CurrentVelocity;
        CurrentVelocity = (RB.position - LastRBPosition) / Time.fixedDeltaTime;
        CurrentAcceleration = CurrentVelocity - previousVelocity;
        LastRBPosition = RB.position;
    }

    private IEnumerator ChangePedState()
    {
        return IdleAnimation(RandomGenerator.NextFloat(IdleTime * 0.5f, IdleTime));
    }

    private IEnumerator IdleAnimation(float duration)
    {
        if (Agent == null || duration == 0f)
        {
            yield break;
        }

        SetPedState(PedestrianState.Idle);
        yield return FixedUpdateManager.WaitForFixedSeconds(duration);
        SetPedState(PedestrianState.Walking);
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
        if (Anim == null)
            return;

        switch (ThisPedState)
        {
            case PedestrianState.Idle:
                Anim.SetFloat("speed", 0.0f);
                break;
            case PedestrianState.Walking:
                Anim.SetFloat("speed", MovementSpeed);
                break;
        }
    }

    private int GetNextTargetIndex(int index)
    {
        return index >= Targets.Count - 1 ? 0 : index + 1;
    }

    private Vector3 GetRandomTargetPosition(int index)
    {
        Vector3 tempV = Vector3.zero;
        if (Targets.Count > 0 && index >= 0 && index < Targets.Count)
        {
            tempV = Targets[index];
        }

        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh && count < 10000)
        {
            Vector3 randomPoint = tempV + RandomGenerator.InsideUnitSphere() * TargetRange;
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

    public void StopPEDCoroutines()
    {
        foreach (Coroutine coroutine in Coroutines)
        {
            if (coroutine != null)
            {
                FixedUpdateManager.StopCoroutine(coroutine);
            }
        }
        Coroutines = new Coroutine[System.Enum.GetNames(typeof(CoroutineID)).Length];
    }

    private void Reset()
    {
        StopPEDCoroutines();
        Path.ClearCorners();
        CurrentWP = 0;
    }

    private void EvaluateDistanceFromFocus()
    {
        if (SimulatorManager.Instance.IsAPI)
            return;

        if (!SimulatorManager.Instance.PedestrianManager.spawnsManager.WithinSpawnArea(transform.position) && !SimulatorManager.Instance.PedestrianManager.spawnsManager.IsVisible(Bounds))
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
        }

        if ((LayerMask.GetMask("Agent", "NPC", "Pedestrian", "Obstacle") & 1 << collision.gameObject.layer) != 0)
        {
            GetNextPath(false);
        }
    }

    // Debug
    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.cyan;
    //    foreach (var item in Path.corners)
    //    {
    //        Gizmos.DrawSphere(item, 0.25f);
    //    }
    //}

    #region network
    protected override string ComponentKey { get; } = "PedestrianController";

    protected override bool PushSnapshot(BytesStack messageContent)
    {
        messageContent.PushEnum<PedestrianState>((int)ThisPedState);
        messageContent.PushEnum<ControlType>((int)Control);
        messageContent.PushFloat(MovementSpeed);
        return true;
    }

    protected override void ApplySnapshot(DistributedMessage distributedMessage)
    {
        MovementSpeed = distributedMessage.Content.PopFloat();
        Control = distributedMessage.Content.PopEnum<ControlType>();
        ThisPedState = distributedMessage.Content.PopEnum<PedestrianState>();
        //Validate animator, as snapshot can be received before it is initialized
        if (Anim != null && Anim.isActiveAndEnabled && Anim.runtimeAnimatorController != null)
        {
            SetAnimationControllerParameters();
        }
    }
    #endregion
}
