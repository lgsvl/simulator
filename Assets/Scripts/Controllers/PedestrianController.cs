/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Simulator.Api;
using Simulator.Map;
using Simulator.Utilities;

public enum PedestrianState
{
    None,
    Idle,
    Walking,
    Crossing
};

public class PedestrianController : MonoBehaviour
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
    private bool isInit = false;
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

    private enum CoroutineID
    {
        ChangePedState = 0,
        IdleAnimation = 1,
    }

    #region api
    public void WalkRandomly(bool enable)
    {
        Reset();

        if (!enable)
        {
            Control = ControlType.Manual;
            thisPedState = PedestrianState.None;
            return;
        }

        agent.avoidancePriority = RandomGenerator.Next(1, 100);

        var position = agent.transform.position;
        MapPedestrian closest = null;
        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        foreach (var path in SimulatorManager.Instance.PedestrianManager.pedPaths)
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

        NextTargetIndex = 0;

        Control = ControlType.Waypoints;
        waypointLoop = loop;
    }

    public void InitManual(Vector3 position, Quaternion rotation, int seed)
    {
        if (SimulatorManager.Instance.IsAPI)
        {
            FixedUpdateManager = ApiManager.Instance;
        }
        else
        {
            FixedUpdateManager = SimulatorManager.Instance.PedestrianManager;
        }

        RandomGenerator = new System.Random(seed);
        Path = new NavMeshPath();

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        Name = transform.GetChild(0).name;

        agent.avoidancePriority = 0;

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.Warp(position);
        agent.transform.rotation = rotation;

        thisPedState = PedestrianState.None;
        Control = ControlType.Manual;

        isInit = true;
    }
    #endregion

    public void InitPed(List<Vector3> pedSpawnerTargets, int seed)
    {
        if (SimulatorManager.Instance.IsAPI)
        {
            FixedUpdateManager = ApiManager.Instance;
        }
        else
        {
            FixedUpdateManager = SimulatorManager.Instance.PedestrianManager;
        }

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

        // get random pos index
        CurrentTargetIndex = RandomGenerator.Next(targets.Count);
        NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
        var initPos = GetRandomTargetPosition(CurrentTargetIndex);

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.Warp(initPos);
        agent.transform.rotation = Quaternion.identity;

        isInit = true;
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

            if (thisPedState == PedestrianState.Idle)
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
                Vector3 targetPos = new Vector3(corners[CurrentWP].x, rb.position.y, corners[CurrentWP].z);
                Vector3 direction = targetPos - rb.position;

                CurrentTurn = direction;
                CurrentSpeed = LinearSpeed;
                thisPedState = PedestrianState.Walking;

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
        }
        else if (Control == ControlType.Waypoints)
        {
            if (thisPedState == PedestrianState.Idle)
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
                Vector3 targetPos = new Vector3(corners[CurrentWP].x, rb.position.y, corners[CurrentWP].z);
                Vector3 direction = targetPos - rb.position;

                CurrentTurn = direction;
                CurrentSpeed = LinearSpeed;
                thisPedState = PedestrianState.Walking;

                if (direction.magnitude < Accuracy)
                {
                    CurrentWP++;
                    if (CurrentWP >= corners.Length)
                    {
                        ApiManager.Instance?.AddWaypointReached(gameObject, NextTargetIndex);
                        Coroutines[(int)CoroutineID.IdleAnimation] = FixedUpdateManager.StartCoroutine(IdleAnimation(idle[NextTargetIndex]));

                        Path.ClearCorners();
                        CurrentTargetIndex = NextTargetIndex;
                        NextTargetIndex = GetNextTargetIndex(CurrentTargetIndex);
                        CurrentWP = 0;

                        if (NextTargetIndex == targets.Count - 1 && !waypointLoop)
                        {
                            WalkRandomly(false);
                        }
                    }
                }
            }
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

    private void Debug(int i=1)
    {
        int frame;
        if (SimulatorManager.Instance.IsAPI)
            frame = ApiManager.Instance.CurrentFrame;
        else
            frame = SimulatorManager.Instance.CurrentFrame;

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

        thisPedState = PedestrianState.Idle;

        yield return FixedUpdateManager.WaitForFixedSeconds(duration);

        thisPedState = PedestrianState.Walking;
    }

    private bool IsRandomIdle()
    {
        if (RandomGenerator.Next(1000) < 1 && thisPedState == PedestrianState.Walking)
        {
            return true;
        }
        return false;
    }

    private void SetAnimationControllerParameters()
    {
        if (agent == null || anim == null) return;

        if (thisPedState == PedestrianState.Walking || thisPedState == PedestrianState.Crossing)
        {
            anim.SetFloat("speed", LinearSpeed);
        }
        else
        {
            anim.SetFloat("speed", 0f);
        }
    }

    private int GetNextTargetIndex(int index)
    {
        return index >= targets.Count - 1 ? 0 : index + 1;
    }

    private Vector3 GetRandomTargetPosition(int index)
    {
        Vector3 tempV = targets[index];

        int count = 0;
        bool isInNavMesh = false;
        while (!isInNavMesh || count > 10000)
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
        thisPedState = PedestrianState.None;
    }
}
