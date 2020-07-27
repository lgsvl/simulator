/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Simulator;
using UnityEngine;
using Simulator.Api;
using Simulator.Map;
using Simulator.Network.Core.Components;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Identification;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using Simulator.Utilities;

[SelectionBase]
public class NPCController : MonoBehaviour, IMessageSender, IMessageReceiver, IGloballyUniquelyIdentified
{
    public NPCBehaviourBase ActiveBehaviour => _ActiveBehaviour;
    private NPCBehaviourBase _ActiveBehaviour;

    #region vars
    [HideInInspector]
    public MeshCollider MainCollider;
    public Vector3 lastRBPosition;
    public Quaternion lastRBRotation;
    public Rigidbody rb;
    public Bounds Bounds;

    public Vector3 simpleVelocity;
    public Vector3 simpleAngularVelocity;
    public Vector3 simpleAcceleration;
    private GameObject wheelColliderHolder;
    class WheelData
    {
        public Transform transform;
        public WheelCollider collider;
        public bool steering;
        public Vector3 origPos;
    }
    private List<WheelData> wheels = new List<WheelData>();

    private float wheelDampingRate = 1f;

    // map data
    public string id { get; set; }

    // targeting
    public Transform frontCenter;
    public Transform frontLeft;
    public Transform frontRight;

    public float currentSpeed;
    public Vector3 steerVector = Vector3.forward;

    public float dampenFactor = 10f; // this value requires tuning
    public float adjustFactor = 10f; // this value requires tuning

    private class IndicatorRenderer
    {
        public Renderer renderer = null;
        public int materialIndex;
        public void SetEmission(float value)
        {
            var mats = renderer.materials;
            mats[materialIndex].SetFloat("_EmitIntensity", value * 12.8f); // nits bc ev100 is bugged in shader
            renderer.materials = mats;
        }
    }

    // emitters
    private List<Renderer> allRenderers;
    private IndicatorRenderer headLight;
    private IndicatorRenderer brakeLight;
    private IndicatorRenderer indicatorLeft;
    private IndicatorRenderer indicatorRight;
    private IndicatorRenderer indicatorReverse;

    // lights
    private Light[] allLights;
    private List<Light> headLights = new List<Light>();
    private List<Light> brakeLights = new List<Light>();
    private List<Light> indicatorLeftLights = new List<Light>();
    private List<Light> indicatorRightLights = new List<Light>();
    private Light indicatorReverseLight;

    //Network
    private MessagesManager messagesManager;
    private string key;
    public string GUID => id;
    public string Key => key ?? (string.IsNullOrEmpty(GUID) ? null : key = $"{GUID}/NPCController");

    private enum NPCLightStateTypes
    {
        Off,
        Low,
        High
    };
    private NPCLightStateTypes currentNPCLightState = NPCLightStateTypes.Off;

    public bool isForcedStop = false;
    public bool isLeftTurn = false;
    public bool isRightTurn = false;
    private IEnumerator turnSignalIE;
    private IEnumerator hazardSignalIE;

    public System.Random RandomGenerator;
    public MonoBehaviour FixedUpdateManager;
    public NPCManager NPCManager;

    public HashSet<Coroutine> Coroutines = new HashSet<Coroutine>();
    protected int agentLayer;
    public uint GTID { get; set; }
    public string NPCLabel { get; set; }

    public NPCSizeType Size { get; set; } = NPCSizeType.MidSize;
    public Color NPCColor { get; set; } = Color.black;
    private int _seed;
    public MapIntersection currentIntersection = null;
    #endregion

    #region mono
    private void Start()
    {
        messagesManager = Loader.Instance.Network.MessagesManager;
        StartCoroutine(WaitForId(() => messagesManager?.RegisterObject(this)));
    }

    private void OnEnable()
    {
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged += OnTimeOfDayChange;
        GetSimulatorTimeOfDay();
        agentLayer = LayerMask.NameToLayer("Agent");
        if (_ActiveBehaviour)
        {
            _ActiveBehaviour.enabled = true;
        }
    }

    private void OnDisable()
    {
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged -= OnTimeOfDayChange;
        if (_ActiveBehaviour!=null)
            _ActiveBehaviour.enabled = false;
    }

    public void PhysicsUpdate()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (wheels.Count < 4)
        {
            Vector3 newLeft = Vector3.Cross(transform.forward, Vector3.up);
            Vector3 desiredUp = Vector3.Cross(transform.forward, newLeft);
            if (desiredUp.y < 0)
            {
                desiredUp = -desiredUp;
            }

            Quaternion delta = Quaternion.FromToRotation(transform.up, desiredUp);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            Vector3 torque = axis * (angle * adjustFactor - rb.angularVelocity.z * dampenFactor);

            rb.AddTorque(torque, ForceMode.Acceleration);
        }

        if (Time.fixedDeltaTime > 0)
        {
            var previousVelocity = simpleVelocity;
            simpleVelocity = (rb.position - lastRBPosition) / Time.fixedDeltaTime;
            simpleAcceleration = simpleVelocity - previousVelocity;

            Vector3 euler1 = lastRBRotation.eulerAngles;
            Vector3 euler2 = rb.rotation.eulerAngles;
            Vector3 diff = euler2 - euler1;
            for (int i = 0; i < 3; i++)
            {
                diff[i] = (diff[i] + 180) % 360 - 180;
            }
            simpleAngularVelocity = diff / Time.fixedDeltaTime * Mathf.Deg2Rad;
            SetLastPosRot(rb.position, rb.rotation);
        }

        if (ActiveBehaviour)
        {
            ActiveBehaviour.PhysicsUpdate();
        }

        if (currentSpeed > 0.1f)
        {
            WheelMovement();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == agentLayer)
        {
            ApiManager.Instance?.AddCollision(rb.gameObject, other.attachedRigidbody.gameObject);
            SimulatorManager.Instance.AnalysisManager.IncrementNPCCollision();
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
            if(_ActiveBehaviour) _ActiveBehaviour.OnAgentCollision(other.gameObject);
        }
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        messagesManager?.UnregisterObject(this);
    }
    #endregion

    #region init
    public void Init(int seed)
    {
        FixedUpdateManager = SimulatorManager.Instance.FixedUpdateManager;
        NPCManager = SimulatorManager.Instance.NPCManager;
        RandomGenerator = new System.Random(seed);
        _seed = seed;
        SetNeededComponents();
        ResetData();
        if (_ActiveBehaviour)
        {
            _ActiveBehaviour.controller = this;
            _ActiveBehaviour.rb = rb;
            _ActiveBehaviour.Init(seed);
        }
    }

    public void InitLaneData(MapLane lane)
    {
        if (_ActiveBehaviour)
        {
            _ActiveBehaviour.InitLaneData(lane);
        }
    }

    private void SetNeededComponents()
    {
        rb = GetComponent<Rigidbody>();
        allRenderers = GetComponentsInChildren<Renderer>().ToList();
        allLights = GetComponentsInChildren<Light>();
        if (allLights.Length == 0)
        {
            Debug.LogError(gameObject.name + " did not find any lights!");
        }

        Color.RGBToHSV(NPCColor, out float h, out float s, out float v);
        h = Mathf.Clamp01(RandomGenerator.NextFloat(h - 0.01f, h + 0.01f));
        v = Mathf.Clamp01(RandomGenerator.NextFloat(v - 0.1f, v + 0.1f));
        NPCColor = Color.HSVToRGB(h, s, v);

        MainCollider = GetComponentInChildren<MeshCollider>();
        
        // wheel collider holder
        wheelColliderHolder = new GameObject("WheelColliderHolder");
        wheelColliderHolder.transform.SetParent(transform.GetChild(0));
        wheelColliderHolder.SetActive(true);

        foreach (Renderer child in allRenderers)
        {
            if (child.name.Contains("Wheel") && !child.name.Contains("Spare"))
            {
                AddWheel(child.transform);
            }

            if (child.name.Contains("Body"))
            {
                var rendererMats = child.materials;
                for (int i = 0; i < rendererMats.Length; i++)
                {
                    if (rendererMats[i].name.Contains("Body"))
                        rendererMats[i].SetColor("_BaseColor", NPCColor);
                }
                if (MainCollider == null)
                {
                    MainCollider = child.gameObject.AddComponent<MeshCollider>();
                    MainCollider.convex = true;
                }
            }
            {
                var rendererMats = child.materials;
                for (int i = 0; i < rendererMats.Length; i++)
                {
                    if (rendererMats[i].name.Contains("LightHead"))
                        headLight = new IndicatorRenderer() { renderer = child, materialIndex = i };
                    if (rendererMats[i].name.Contains("LightBrake"))
                        brakeLight = new IndicatorRenderer() { renderer = child, materialIndex = i };
                    if (rendererMats[i].name.Contains("IndicatorLeft"))
                        indicatorLeft = new IndicatorRenderer() { renderer = child, materialIndex = i };
                    if (rendererMats[i].name.Contains("IndicatorRight"))
                        indicatorRight = new IndicatorRenderer() { renderer = child, materialIndex = i };
                    if (rendererMats[i].name.Contains("IndicatorReverse"))
                        indicatorReverse = new IndicatorRenderer() { renderer = child, materialIndex = i };
                }
            }
        }

        MainCollider.enabled = true;
        MainCollider.gameObject.layer = LayerMask.NameToLayer("NPC");

        foreach (Light light in allLights)
        {
            if (light.name.Contains("Head"))
            {
                headLights.Add(light);
            }
            else if (light.name.Contains("Brake"))
            {
                brakeLights.Add(light);
            }
            else if (light.name.Contains("IndicatorLeft"))
            {
                indicatorLeftLights.Add(light);
            }
            else if (light.name.Contains("IndicatorRight"))
            {
                indicatorRightLights.Add(light);
            }
            else if (light.name.Contains("IndicatorReverse"))
            {
                indicatorReverseLight = light;
            }
        }

        if (headLights.Count == 0)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing light 'Head'");
        }
        if (brakeLights.Count == 0)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing light 'Brake'");
        }
        if (indicatorLeftLights.Count == 0)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing light 'IndicatorLeft'");
        }
        if (indicatorRightLights.Count == 0)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing light 'IndicatorRight'");
        }
        if (indicatorReverseLight == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing light 'IndicatorReverse'");
        }
        if (MainCollider == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing renderer 'Body'");
        }
        if (headLight == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing material 'LightHead'");
        }
        if (brakeLight == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing material 'LightBrake'");
        }
        if (indicatorRight == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing material 'IndicatorRight'");
        }
        if (indicatorLeft == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing material 'IndicatorLeft'");
        }
        if (indicatorReverse == null)
        {
            Debug.LogWarning($"Asset {gameObject.name} missing material 'IndicatorReverse'");
        }

        Bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in allRenderers)
        {
            Bounds.Encapsulate(renderer.bounds); // renderer.bounds is world space 
        }

        // centerOfMass is relative to the transform origin
        if (wheels.Count < 4 || name.Contains("Trailer"))
        {
            rb.centerOfMass = Bounds.center + new Vector3(0, -Bounds.extents.y * 0.15f , 0);
        }
        else
        {
            rb.centerOfMass = Bounds.center + new Vector3(0, 0, Bounds.extents.z * 0.3f);
        }

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(Bounds.center.x, Bounds.center.y, Bounds.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");

        // front transforms
        GameObject go = new GameObject("Front");
        go.transform.position = new Vector3(Bounds.center.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontCenter = go.transform;
        go = new GameObject("Right");
        go.transform.position = new Vector3(Bounds.center.x + Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontRight = go.transform;
        go = new GameObject("Left");
        go.transform.position = new Vector3(Bounds.center.x - Bounds.max.x, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        go.transform.SetParent(transform, true);
        frontLeft = go.transform;
    }

    public T SetBehaviour<T>() where T: NPCBehaviourBase
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

        T behaviour = gameObject.AddComponent<T>();

        _ActiveBehaviour = behaviour;
        _ActiveBehaviour.controller = this;
        _ActiveBehaviour.rb = rb;
        _ActiveBehaviour.Init(_seed);
        return behaviour;
    }

    void AddWheel(Transform wheel)
    {
        GameObject go = new GameObject("Collider for " + wheel.name);
        go.transform.SetParent(wheelColliderHolder.transform);
        WheelCollider wheelCollider = go.AddComponent<WheelCollider>();
        wheelCollider.mass = 30f;
        wheelCollider.suspensionDistance = 0.3f;
        wheelCollider.forceAppPointDistance = 0.2f;
        wheelCollider.suspensionSpring = new JointSpring { spring = 35000f, damper = 8000, targetPosition = 0.5f };
        wheelCollider.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.5f, stiffness = 1.2f };
        wheelCollider.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.2f, extremumValue = 1f, asymptoteSlip = 1.5f, asymptoteValue = 1f, stiffness = 2.2f };
        go.transform.position = wheel.position;
        wheelCollider.center = new Vector3(0f, go.transform.localPosition.y / 2, 0f);
        wheelCollider.radius = wheel.GetComponent<MeshFilter>().mesh.bounds.extents.z;
        wheelCollider.ConfigureVehicleSubsteps(5.0f, 30, 10);
        wheelCollider.wheelDampingRate = wheelDampingRate;

        var data = new WheelData()
        {
            transform = wheel,
            collider = wheelCollider,
            origPos = wheelColliderHolder.transform.InverseTransformPoint(wheel.position),
            steering = wheel.name.Contains("Front")
        };
        wheels.Add(data);
        DistributeTransform(wheel);
    }

    // api
    public void SetLastPosRot(Vector3 pos, Quaternion rot)
    {
        lastRBPosition = pos;
        lastRBRotation = rot;
    }
    #endregion

    #region spawn
    public void StopNPCCoroutines()
    {
        if (FixedUpdateManager == null)
            return;

        foreach (Coroutine coroutine in Coroutines)
        {
            if (coroutine != null)
            {
                FixedUpdateManager.StopCoroutine(coroutine);
            }
        }
    }

    private void ResetData()
    {
        StopNPCCoroutines();
        ResetLights();
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        isLeftTurn = false;
        isRightTurn = false;
        isForcedStop = false;
        simpleVelocity = Vector3.zero;
        simpleAngularVelocity = Vector3.zero;
        lastRBPosition = transform.position;
        lastRBRotation = transform.rotation;
        currentIntersection = null;
    }
    #endregion

    #region physics
    public Vector3 GetVelocity()
    {
        return simpleVelocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return simpleAngularVelocity;
    }
    #endregion

    #region inputs
    public void ForceEStop(bool isStop)
    {
        isForcedStop = isStop;
    }
    #endregion

    #region lights
    public void GetSimulatorTimeOfDay()
    {
        switch (SimulatorManager.Instance.EnvironmentEffectsManager.CurrentTimeOfDayState)
        {
            case TimeOfDayStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
        }
        SetLights((int)currentNPCLightState);
    }

    private void OnTimeOfDayChange(TimeOfDayStateTypes state)
    {
        switch (state)
        {
            case TimeOfDayStateTypes.Day:
                currentNPCLightState = NPCLightStateTypes.Off;
                break;
            case TimeOfDayStateTypes.Night:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunrise:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
            case TimeOfDayStateTypes.Sunset:
                currentNPCLightState = NPCLightStateTypes.Low;
                break;
        }
        SetLights((int)currentNPCLightState);
    }

    public void SetLights(int state)
    {
        currentNPCLightState = (NPCLightStateTypes)state;
        SetHeadLights();
        SetRunningLights();

        if (Loader.Instance.Network.IsMaster)
        {
            var message = MessagesPool.Instance.GetMessage(8);
            message.AddressKey = Key;
            message.Content.PushInt(state);
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetLights);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }

    private void SetHeadLights()
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                foreach (var light in headLights)
                    light.enabled = false;
                headLight?.SetEmission(0f);
                break;
            case NPCLightStateTypes.Low:
                foreach (var light in headLights)
                {
                    light.enabled = true;
                }
                headLight?.SetEmission(10f);
                break;
            case NPCLightStateTypes.High:
                foreach (var light in headLights)
                {
                    light.enabled = true;
                }
                headLight?.SetEmission(12f);
                break;
        }
    }

    private void SetRunningLights()
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                foreach (var light in brakeLights)
                    light.enabled = false;
                brakeLight?.SetEmission(0f);
                break;
            case NPCLightStateTypes.Low:
            case NPCLightStateTypes.High:
                foreach (var light in brakeLights)
                {
                    light.enabled = true;
                }
                brakeLight?.SetEmission(3f);
                break;
        }
    }

    public void SetBrakeLights(bool state)
    {
        switch (currentNPCLightState)
        {
            case NPCLightStateTypes.Off:
                if (state)
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                    }
                    brakeLight?.SetEmission(10f);
                }
                else
                {
                    foreach (var light in brakeLights)
                        light.enabled = false;
                    brakeLight?.SetEmission(0f);
                }
                break;
            case NPCLightStateTypes.Low:
            case NPCLightStateTypes.High:
                if (state)
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                    }
                    brakeLight?.SetEmission(10f);
                }
                else
                {
                    foreach (var light in brakeLights)
                    {
                        light.enabled = true;
                    }
                    brakeLight?.SetEmission(3f);
                }
                break;
        }

        if (Loader.Instance.Network.IsMaster)
        {
            var message = MessagesPool.Instance.GetMessage(5);
            message.AddressKey = Key;
            message.Content.PushBool(state);
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetBrakeLights);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }

    public void SetNPCTurnSignal(bool isForced = false, bool isLeft = false, bool isRight = false)
    {
        if (isForced)
        {
            isLeftTurn = isLeft;
            isRightTurn = isRight;
        }

        if (turnSignalIE != null)
        {
            FixedUpdateManager.StopCoroutine(turnSignalIE);
        }

        turnSignalIE = StartTurnSignal();

        Coroutines.Add(FixedUpdateManager.StartCoroutine(turnSignalIE));

        if (Loader.Instance.Network.IsMaster)
        {
            //Force setting turn signals on clients
            var message = MessagesPool.Instance.GetMessage(7);
            message.AddressKey = Key;
            message.Content.PushBool(isRightTurn);
            message.Content.PushBool(isLeftTurn);
            message.Content.PushBool(true);
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetNPCTurnSignal);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }

    public void SetNPCHazards(bool state = false)
    {
        if (hazardSignalIE != null)
        {
            FixedUpdateManager.StopCoroutine(hazardSignalIE);
        }

        isLeftTurn = state;
        isRightTurn = state;

        if (state)
        {
            hazardSignalIE = StartHazardSignal();
            Coroutines.Add(FixedUpdateManager.StartCoroutine(hazardSignalIE));
        }

        if (Loader.Instance.Network.IsMaster)
        {
            var message = MessagesPool.Instance.GetMessage(5);
            message.AddressKey = Key;
            message.Content.PushBool(state);
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetNPCHazards);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }

    private IEnumerator StartTurnSignal()
    {
        while (isLeftTurn || isRightTurn)
        {
            SetTurnIndicator(true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
        }
        SetTurnIndicator(isReset: true);
    }

    private IEnumerator StartHazardSignal()
    {
        while (isLeftTurn && isRightTurn)
        {
            SetTurnIndicator(true, isHazard: true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
            SetTurnIndicator(false, isHazard: true);
            yield return FixedUpdateManager.WaitForFixedSeconds(0.5f);
        }
        SetTurnIndicator(isReset: true);
    }

    private void SetTurnIndicator(bool state = false, bool isReset = false, bool isHazard = false)
    {
        float emit = state ? 10f : 0f;
        if (isHazard)
        {
            indicatorLeft?.SetEmission(emit);
            indicatorRight?.SetEmission(emit);
            foreach (var light in indicatorLeftLights)
                light.enabled = state;
            foreach (var light in indicatorRightLights)
                light.enabled = state;
        }
        else
        {
            (isLeftTurn ? indicatorLeft : indicatorRight)?.SetEmission(emit);
            foreach (var light in isLeftTurn ? indicatorLeftLights : indicatorRightLights)
                light.enabled = state;
        }

        if (isReset)
        {
            indicatorLeft?.SetEmission(0f);
            indicatorRight?.SetEmission(0f);
            foreach (var light in indicatorLeftLights)
                light.enabled = false;
            foreach (var light in indicatorRightLights)
                light.enabled = false;
        }
    }

    public void SetIndicatorReverse(bool state)
    {
        indicatorReverse?.SetEmission(state ? 8f : 0f);
        if (indicatorReverseLight != null)
        {
            indicatorReverseLight.enabled = state;
        }

        if (Loader.Instance.Network.IsMaster)
        {
            var message = MessagesPool.Instance.GetMessage(5);
            message.AddressKey = Key;
            message.Content.PushBool(state);
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.SetIndicatorReverse);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }

    public void ResetLights()
    {
        currentNPCLightState = NPCLightStateTypes.Off;
        SetHeadLights();
        SetRunningLights();
        SetBrakeLights(false);
        if (FixedUpdateManager != null)
        {
            if (turnSignalIE != null) FixedUpdateManager.StopCoroutine(turnSignalIE);
            if (hazardSignalIE != null) FixedUpdateManager.StopCoroutine(hazardSignalIE);
        }

        SetTurnIndicator(isReset: true);
        SetIndicatorReverse(false);

        if (Loader.Instance.Network.IsMaster)
        {
            var message = MessagesPool.Instance.GetMessage(4);
            message.AddressKey = Key;
            message.Content.PushEnum<NPCControllerMethodName>((int)NPCControllerMethodName.ResetLights);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
        }
    }
    #endregion

    #region utility
    public void WheelMovement()
    {
        float theta = (simpleVelocity.magnitude * Time.fixedDeltaTime / wheels[0].collider.radius) * Mathf.Rad2Deg;
        Quaternion finalQ = Quaternion.LookRotation(steerVector);
        Vector3 finalE = finalQ.eulerAngles;
        finalQ = Quaternion.Euler(0f, finalE.y, 0f);

        foreach (var wheel in wheels)
        {
            MoveWheel(wheel.transform, wheel.collider, wheel.origPos, theta, finalQ, wheel.steering);
        }
    }

    private void MoveWheel(Transform wheel, WheelCollider collider, Vector3 origPos, float theta, Quaternion Q, bool steering)
    {
        if (wheel.localPosition != origPos)
        {
            wheel.localPosition = origPos;
        }

        if (steering)
        {
            wheel.rotation = Quaternion.RotateTowards(wheel.rotation, Q, Time.fixedDeltaTime * 50f);
        }
        
        wheel.transform.Rotate(Vector3.right, theta, Space.Self);

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        wheel.position = pos;
//      this does not work because we move our NPCs without forces
//        wheel.rotation = rot;
    }
    #endregion

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == agentLayer)
        {
            ApiManager.Instance?.AddCollision(gameObject, collision.gameObject, collision);
            SimulatorManager.Instance.AnalysisManager.IncrementNPCCollision();
            SIM.LogSimulation(SIM.Simulation.NPCCollision);
            ActiveBehaviour?.OnAgentCollision(collision.gameObject);
        }
    }

    #region network
    /// <summary>
    /// Method waiting while the GUID in the guidSource is not set
    /// </summary>
    /// <param name="callback">Callback called after GUID is set</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator WaitForId(Action callback)
    {
        while (string.IsNullOrEmpty(id))
            yield return null;
        callback();
    }

    /// <summary>
    /// Adds required components to make transform distributed from master to clients
    /// </summary>
    /// <param name="transformToDistribute">Transform that will be distributed</param>
    private void DistributeTransform(Transform transformToDistribute)
    {
        if (transformToDistribute.gameObject.GetComponent<DistributedTransform>() == null)
            transformToDistribute.gameObject.AddComponent<DistributedTransform>();
    }

    /// <inheritdoc/>
    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        var methodName = distributedMessage.Content.PopEnum<NPCControllerMethodName>();
        switch (methodName)
        {
            case NPCControllerMethodName.SetLights:
                SetLights(distributedMessage.Content.PopInt());
                break;
            case NPCControllerMethodName.SetBrakeLights:
                SetBrakeLights(distributedMessage.Content.PopBool());
                break;
            case NPCControllerMethodName.SetNPCTurnSignal:
                SetNPCTurnSignal(distributedMessage.Content.PopBool(), distributedMessage.Content.PopBool(), distributedMessage.Content.PopBool());
                break;
            case NPCControllerMethodName.SetNPCHazards:
                SetNPCHazards(distributedMessage.Content.PopBool());
                break;
            case NPCControllerMethodName.SetIndicatorReverse:
                SetIndicatorReverse(distributedMessage.Content.PopBool());
                break;
            case NPCControllerMethodName.ResetLights:
                ResetLights();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(key))
        {
            messagesManager?.UnicastMessage(endPoint, distributedMessage);
        }
    }

    /// <inheritdoc/>
    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        if (!string.IsNullOrEmpty(key))
        {
            messagesManager?.BroadcastMessage(distributedMessage);
        }
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    private enum NPCControllerMethodName
    {
        SetLights = 0,
        SetBrakeLights = 1,
        SetNPCTurnSignal = 2,
        SetNPCHazards = 3,
        SetIndicatorReverse = 4,
        ResetLights = 5
    }
    #endregion
}

public abstract class NPCBehaviourBase : MonoBehaviour
{
    public NPCController controller;

    public bool autonomous = true;
    public uint GTID { get => controller.GTID; }

    public bool isLeftTurn
    {
        get => controller.isLeftTurn;
        set { controller.isLeftTurn = value; }
    }
    public bool isRightTurn
    {
        get => controller.isRightTurn;
        set { controller.isRightTurn = value; }
    }

    // physics
    [HideInInspector]
    public Rigidbody rb;

    // targeting
    public float currentSpeed
    {
        get => controller.currentSpeed;
        set { controller.currentSpeed = value; }
    }

    public bool isForcedStop
    {
        get => controller.isForcedStop;
        set { controller.isForcedStop = value; }
    }

    protected System.Random RandomGenerator { get => controller.RandomGenerator; }
    protected MonoBehaviour FixedUpdateManager { get => controller.FixedUpdateManager; }
    protected NPCManager NPCManager {get => controller.NPCManager; }
    
    public abstract void PhysicsUpdate();
    public abstract void InitLaneData(MapLane lane);
    public abstract void Init(int seed);
    public abstract void OnAgentCollision(GameObject go);
}
