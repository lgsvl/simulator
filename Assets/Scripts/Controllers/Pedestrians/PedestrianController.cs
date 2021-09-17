/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Simulator;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Identification;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Utilities;
using Random = System.Random;

public class PedestrianController : DistributedComponent, ITriggerAgent, IGloballyUniquelyIdentified
{
    public enum PedestrianState
    {
        Idle,
        Walking,
    };
    
    public PedestrianBehaviourBase ActiveBehaviour => _ActiveBehaviour;
    private PedestrianBehaviourBase _ActiveBehaviour;
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

    private int Seed;
    private int AgentLayer;

    private float IdleTime = 3f;
    private Animator Anim;
    private float CurrentSpeed;

    public float Accuracy { get; } = 0.5f;
    public float TargetRange { get; } =  1f;

    
    public Transform AgentTransform => transform;
    public Bounds Bounds { get; private set; }
    public Vector3 Acceleration => CurrentAcceleration;
    public uint GTID { get; set; }
    public string GUID { get; set; }
    public Random RandomGenerator { get; private set; }
    public MonoBehaviour FixedUpdateManager { get; private set; }
    public Coroutine[] Coroutines { get; private set; } = new Coroutine[Enum.GetNames(typeof(CoroutineID)).Length];
    public MapPedestrianLane MapPath { get; set; }
    public Vector3 CurrentTurn { get; set; }
    public int CurrentWP { get; set; }
    public int CurrentTargetIndex { get; set; }
    public int NextTargetIndex { get; set; }
    public List<Vector3> Targets { get; set; }

    public float MovementSpeed
    {
        get => CurrentSpeed;
        set
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

    public enum CoroutineID
    {
        ChangePedState = 0,
        IdleAnimation = 1,
        WaitForAgent = 2,
    }

    public void PhysicsUpdate()
    {
        if (!enabled)
            return;

        if (ActiveBehaviour)
        {
            ActiveBehaviour.PhysicsUpdate();
        }
    }

    protected override void OnEnable()
    {
        AgentLayer = LayerMask.NameToLayer("Agent");
        if (_ActiveBehaviour != null)
        {
            _ActiveBehaviour.enabled = true;
        }
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (_ActiveBehaviour)
        {
            _ActiveBehaviour.enabled = false;
        }
        base.OnDisable();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == AgentLayer)
        {
            if(_ActiveBehaviour) _ActiveBehaviour.OnAgentCollision(other.gameObject);
        }
    }

    public void SetPedState(PedestrianState state)
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

    public void InitPed(Vector3 position, int spawnIndex, List<Vector3> pedSpawnerTargets, int seed, MapPedestrianLane mapPath = null)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(seed);
        MapPath = mapPath;
        Reset();

        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        RB.position = position;
        Targets = pedSpawnerTargets;
        Name = transform.GetChild(0).name;
        GTID = ++SimulatorManager.Instance.GTIDs;
        Seed = seed;

        CurrentTargetIndex = NextTargetIndex = spawnIndex; // TODO ++Nextindex

        if (RandomGenerator.Next(2) == 0)
        {
            Targets.Reverse();
        }

        SetPedState(PedestrianState.Walking);
        if (ActiveBehaviour != null)
        {
            ActiveBehaviour.controller = this;
            ActiveBehaviour.RB = RB;
            ActiveBehaviour.Init(seed);
        }
        else
        {
            SetBehaviour<PedestrianAutomaticBehaviour>();
            ActiveBehaviour.Init(seed);
        }
    }

    public T SetBehaviour<T>() where T: PedestrianBehaviourBase
    {
        if (ActiveBehaviour as T != null)
        {
            return _ActiveBehaviour as T;
        }

        if (_ActiveBehaviour != null)
        {
            Destroy(_ActiveBehaviour);
            _ActiveBehaviour = null;
        }

        if (typeof(T).IsAbstract)
        {
            return null;
        }

        T behaviour = gameObject.AddComponent<T>();

        _ActiveBehaviour = behaviour;
        _ActiveBehaviour.controller = this;
        _ActiveBehaviour.RB = RB;
        _ActiveBehaviour.Init(Seed);
        return behaviour;
    }

    #region api
    public void InitAPIPed(PedestrianManager.PedSpawnData data)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        RandomGenerator = new System.Random(data.Seed);
        Reset();
 
        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        transform.position = data.Position;
        transform.rotation = data.Rotation;
        RB.position = data.Position;
        RB.rotation = data.Rotation;
        RB.isKinematic = false;
        Name = transform.GetChild(0).name;
        GTID = ++SimulatorManager.Instance.GTIDs;

        SetPedState(PedestrianState.Idle);
        SetBehaviour<PedestrianBehaviourBase>();
    }

    public void WalkRandomly(bool enable)
    {
        Reset();

        if (!enable)
        {
            SetBehaviour<PedestrianBehaviourBase>();
            SetPedState(PedestrianState.Idle);
            return;
        }

        SetPedState(PedestrianState.Walking);
        var behaviour = SetBehaviour<PedestrianAutomaticBehaviour>();
        behaviour.WalkRandomly();
    }

    public void SetSpeed(float speed)
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.SetSpeed(speed);
    }

    #endregion

    public IEnumerator ChangePedState()
    {
        return IdleAnimation(RandomGenerator.NextFloat(IdleTime * 0.5f, IdleTime));
    }

    public IEnumerator IdleAnimation(float duration)
    {
        // TODO (Agent == null || duration == 0f)
        if (duration == 0f)
        {
            yield break;
        }

        SetPedState(PedestrianState.Idle);
        yield return FixedUpdateManager.WaitForFixedSeconds(duration);
        SetPedState(PedestrianState.Walking);
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

    public int GetNextTargetIndex(int index)
    {
        return index >= Targets.Count - 1 ? 0 : index + 1;
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
        Coroutines = new Coroutine[Enum.GetNames(typeof(CoroutineID)).Length];
    }

    public void Reset()
    {
        if (ActiveBehaviour!=null)
            ActiveBehaviour.Reset();
        StopPEDCoroutines();
        CurrentWP = 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Agent"))
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementPedCollision();
            ActiveBehaviour.OnAgentCollision(collision.gameObject);
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
        messageContent.PushFloat(MovementSpeed);
        return true;
    }

    protected override void ApplySnapshot(DistributedMessage distributedMessage)
    {
        MovementSpeed = distributedMessage.Content.PopFloat();
        ThisPedState = distributedMessage.Content.PopEnum<PedestrianState>();
        //Validate animator, as snapshot can be received before it is initialized
        if (Anim != null && Anim.isActiveAndEnabled && Anim.runtimeAnimatorController != null)
        {
            SetAnimationControllerParameters();
        }
    }
    #endregion
}

public abstract class PedestrianBehaviourBase : MonoBehaviour
{
    public PedestrianController controller;

    public uint GTID { get => controller.GTID; }

    // physics
    [HideInInspector]
    public Rigidbody RB;

    protected System.Random RandomGenerator { get => controller.RandomGenerator; }
    protected MonoBehaviour FixedUpdateManager { get => controller.FixedUpdateManager; }
    
    public abstract void PhysicsUpdate();
    public abstract void SetSpeed(float speed);
    public abstract void Init(int seed);
    public abstract void InitAPI(PedestrianManager.PedSpawnData data);
    public abstract void OnAgentCollision(GameObject go);
    public abstract void Reset();
}
