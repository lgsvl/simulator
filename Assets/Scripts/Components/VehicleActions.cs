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
using System.Net;
using Simulator;
using Simulator.Network.Core;
using Simulator.Network.Core.Connection;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Core.Messaging.Data;
using UnityEngine;
using UnityEngine.VFX;

public class VehicleActions : MonoBehaviour, IVehicleActions, IMessageSender, IMessageReceiver
{
    public Texture lowCookie;
    public Texture highCookie;

    private bool isInitialized;
    private IAgentController Controller;
    private Rigidbody RB;

    private Renderer headLightRenderer;
    private Renderer brakeLightRenderer;
    private Renderer indicatorLeftLightRenderer;
    private Renderer indicatorRightLightRenderer;
    private Renderer indicatorReverseLightRenderer;
    private Renderer fogLightRenderer;

    private List<Light> headLights = new List<Light>();
    private List<Light> brakeLights = new List<Light>();
    private List<Light> indicatorLeftLights = new List<Light>();
    private List<Light> indicatorRightLights = new List<Light>();
    private List<Light> indicatorReverseLights = new List<Light>();
    private List<Light> fogLights = new List<Light>();
    private List<Light> interiorLights = new List<Light>();

    public struct TireSprayData
    {
        public WheelCollider wheelCollider;
        public VisualEffect visualEffect;
    };

    private float tireSpraySpawnRate;
    private List<TireSprayData> TireSprayDatas = new List<TireSprayData>();
    private WheelHit WheelColliderHit;
    private Quaternion WheelColliderRot;
    private EnvironmentEffectsManager EnviroManager;

    //Network
    private MessagesManager messagesManager;
    private string key;
    public string Key => key ??= $"{HierarchyUtilities.GetPath(transform)}VehicleActions";
    
    private HeadLightState _currentHeadLightState = HeadLightState.OFF;
    public HeadLightState CurrentHeadLightState
    {
        get => _currentHeadLightState;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            _currentHeadLightState = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    headLights.ForEach(x => x.enabled = false);
                    headLights.ForEach(x => x.intensity = 0f);
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                    }
                    break;
                case HeadLightState.LOW:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 1500f);
                    if (lowCookie != null)
                    {
                        headLights.ForEach(x => x.cookie = lowCookie);
                    }
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 15f);
                    }
                    break;
                case HeadLightState.HIGH:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 2500f);
                    if (highCookie != null)
                    {
                        headLights.ForEach(x => x.cookie = highCookie);
                    }
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 30f);
                    }
                    break;
            }

            if (isInitialized && Loader.Instance.Network.IsMaster)
            {
                var message = MessagesPool.Instance.GetMessage(8);
                message.AddressKey = Key;
                message.Content.PushEnum<HeadLightState>((int)value);
                message.Content.PushEnum<VehicleActionsPropertyName>((int)VehicleActionsPropertyName.CurrentHeadLightState);
                message.Type = DistributedMessageType.ReliableOrdered;
                ((IMessageSender) this).BroadcastMessage(message);
            }
        }
    }

    private WiperState _currentWiperState = WiperState.OFF;
    public WiperState CurrentWiperState
    {
        get => _currentWiperState;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            _currentWiperState = value;

            if (isInitialized && Loader.Instance.Network.IsMaster)
            {
                var message = MessagesPool.Instance.GetMessage(8);
                message.AddressKey = Key;
                message.Content.PushEnum<WiperState>((int)value);
                message.Content.PushEnum<VehicleActionsPropertyName>((int)VehicleActionsPropertyName.CurrentWiperState);
                message.Type = DistributedMessageType.ReliableOrdered;
                ((IMessageSender) this).BroadcastMessage(message);
            }
        }
    }

    private bool _leftTurnSignal = false;
    public bool LeftTurnSignal
    {
        get => _leftTurnSignal;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            var valueChanged = _leftTurnSignal != value;
            _leftTurnSignal = value;
            _rightTurnSignal = _hazardLights = false;
            StartIndicatorLeftStatus();
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.LeftTurnSignal, value);
        }
    }

    private bool _rightTurnSignal = false;
    public bool RightTurnSignal
    {
        get => _rightTurnSignal;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            var valueChanged = _rightTurnSignal != value;
            _rightTurnSignal = value;
            _leftTurnSignal = _hazardLights = false;
            StartIndicatorRightStatus();
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.RightTurnSignal, value);
        }
    }

    private bool _hazardLights = false;
    public bool HazardLights
    {
        get => _hazardLights;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            var valueChanged = _hazardLights != value;
            _hazardLights = value;
            _leftTurnSignal = _rightTurnSignal = false;
            StartIndicatorHazardStatus();
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.HazardLights, value);
        }
    }

    private bool _brakeLights = false;
    public bool BrakeLights
    {
        get => _brakeLights;
        set
        {
            var valueChanged = _brakeLights != value;
            _brakeLights = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    if (brakeLightRenderer != null)
                    {
                        brakeLights.ForEach(x => x.intensity = _brakeLights ? 400f : 0f);
                        brakeLightRenderer.material.SetFloat("_EmitIntensity", _brakeLights ? 8f : 0f);
                    }
                    break;
                case HeadLightState.LOW:
                case HeadLightState.HIGH:
                    if (brakeLightRenderer != null)
                    {
                        brakeLights.ForEach(x => x.intensity = _brakeLights ? 400f : 80f);
                        brakeLightRenderer.material.SetFloat("_EmitIntensity", _brakeLights ? 8f : 2f);
                    }
                    break;
            }
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.BrakeLights, value);
        }
    }

    private bool _fogLights = false;
    public bool FogLights
    {
        get => _fogLights;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            var valueChanged = _fogLights != value;
            _fogLights = value;
            fogLights.ForEach(x => x.enabled = _fogLights);
            fogLights.ForEach(x => x.intensity = _fogLights ? 300f : 0f);
            if (fogLightRenderer != null)
            {
                fogLightRenderer.material.SetFloat("_EmitIntensity", _fogLights ? 10f : 0f);
            }
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.FogLights, value);
        }
    }

    private bool _reverseLights = false;
    public bool ReverseLights
    {
        get => _reverseLights;
        set
        {
            var valueChanged = _reverseLights != value;
            _reverseLights = value;
            indicatorReverseLights.ForEach(x => x.enabled = _reverseLights);
            indicatorReverseLights.ForEach(x => x.intensity = _reverseLights ? 400f : 0f);
            if (indicatorReverseLightRenderer != null)
            {
                indicatorReverseLightRenderer.material.SetFloat("_EmitIntensity", _reverseLights ? 6f : 0f);
            }
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.ReverseLights, value);
        }
    }

    private bool _interiorLights = false;
    public bool InteriorLight
    {
        get => _interiorLights;
        set
        {
            if (!Controller.Active && !Loader.Instance.Network.IsClient)
                return;

            var valueChanged = _interiorLights != value;
            _interiorLights = value;
            interiorLights.ForEach(x => x.enabled = _interiorLights);
            
            if (valueChanged)
                BroadcastProperty(VehicleActionsPropertyName.InteriorLight, value);
        }
    }

    private IEnumerator indicatorLeftIE;
    private IEnumerator indicatorRightIE;
    private IEnumerator indicatorHazardIE;

    private void Awake()
    {
        SetNeededComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;
        
        messagesManager = Loader.Instance.Network.MessagesManager;
        messagesManager?.RegisterObject(this);
        RB = GetComponent<Rigidbody>();
        EnviroManager = SimulatorManager.Instance.EnvironmentEffectsManager;
        isInitialized = true;
        BroadcastStateSnapshot();
    }

    private void Update()
    {
        //Update visual effects transform
        foreach (var item in TireSprayDatas)
        {
            if (item.wheelCollider.GetGroundHit(out WheelColliderHit))
            {
                item.visualEffect.transform.position = WheelColliderHit.point;
                item.wheelCollider.GetWorldPose(out var pos, out WheelColliderRot);
                item.visualEffect.transform.rotation = WheelColliderRot;
            }
        }

        //Update visual effects spawn rate
        if (EnviroManager != null && RB != null)
        {
            var localVel = transform.InverseTransformDirection(RB.velocity);
            var lerp = 0f;

            if (EnviroManager.Wet <= 0.25f)
            {
                SetTireSpraySpawnRate(0.0f);
            }
            else if (EnviroManager.Wet > 0.25f && EnviroManager.Wet <= 0.5f)
            {
                lerp = localVel.z > 0.0f ? Mathf.InverseLerp(0.0f, 100.0f, localVel.z) : 0.0f;
                SetTireSpraySpawnRate(lerp);
            }
            else if (EnviroManager.Wet > 0.5f && EnviroManager.Wet <= 0.75f)
            {
                lerp = localVel.z > 0.0f ? Mathf.InverseLerp(0.0f, 75.0f, localVel.z) : 0.0f;
                SetTireSpraySpawnRate(lerp);
            }
            else if (EnviroManager.Wet > 0.75f && EnviroManager.Wet <= 1.0f)
            {
                lerp = localVel.z > 0.0f ? Mathf.InverseLerp(0.0f, 25.0f, localVel.z) : 0.0f;
                SetTireSpraySpawnRate(lerp);
            }
        }
    }

    private void SetTireSpraySpawnRate(float spawnRate, bool forceSet = false)
    {
        if (!forceSet && Mathf.Approximately(spawnRate, tireSpraySpawnRate))
            return;
        
        tireSpraySpawnRate = spawnRate;
        foreach (var item in TireSprayDatas)
        {
            item.visualEffect.SetFloat("_SpawnRate", spawnRate);
        }
        BroadcastProperty(VehicleActionsPropertyName.TireSpraySpawnRate, spawnRate);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        Deinitialize();
    }

    private void Deinitialize()
    {
        if (!isInitialized)
            return;
        
        messagesManager?.UnregisterObject(this);
        isInitialized = false;
    }

    private void SetNeededComponents()
    {
        var dynamics = GetComponent<VehicleSMI>();
        Controller = GetComponent<IAgentController>();
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var animators = GetComponentsInChildren<Animator>(true); // TODO wipers doors windows

        var bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer child in allRenderers)
        {
            if (child.name == "HeadLights")
            {
                headLightRenderer = child;
                if (headLightRenderer != null)
                {
                    headLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            if (child.name == "BrakeLights")
            {
                brakeLightRenderer = child;
                if (brakeLightRenderer != null)
                {
                    brakeLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            if (child.name == "LeftTurnIndicator")
            {
                indicatorLeftLightRenderer = child;
                if (indicatorLeftLightRenderer != null)
                {
                    indicatorLeftLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            if (child.name == "RightTurnIndicator")
            {
                indicatorRightLightRenderer = child;
                if (indicatorRightLightRenderer != null)
                {
                    indicatorRightLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            if (child.name == "ReverseIndicator")
            {
                indicatorReverseLightRenderer = child;
                if (indicatorReverseLightRenderer != null)
                {
                    indicatorReverseLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            if (child.name == "FogLights")
            {
                fogLightRenderer = child;
                if (fogLightRenderer != null)
                {
                    fogLightRenderer.material.SetFloat("_EmitIntensity", 0f);
                }
            }
            bounds.Encapsulate(child.bounds);
        }

        Controller.Bounds = bounds;

        CreateCinematicTransforms();
        CreateDriverViewTransform();

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, bounds.size.y / 2, gtBoxCollider.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");

        foreach (Transform t in transform)
        {
            if (t.name == "HeadLights")
                headLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "BrakeLights")
                brakeLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "IndicatorsLeft")
                indicatorLeftLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "IndicatorsRight")
                indicatorRightLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "IndicatorsReverse")
                indicatorReverseLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "FogLights")
                fogLights.AddRange(t.GetComponentsInChildren<Light>());
            if (t.name == "InteriorLights")
                interiorLights.AddRange(t.GetComponentsInChildren<Light>());
        }

        headLights?.ForEach(x => x.enabled = false);
        brakeLights?.ForEach(x => x.enabled = true);
        brakeLights.ForEach(x => x.intensity = 0f);
        indicatorLeftLights?.ForEach(x => x.enabled = false);
        indicatorRightLights?.ForEach(x => x.enabled = false);
        indicatorReverseLights?.ForEach(x => x.enabled = false);
        fogLights?.ForEach(x => x.enabled = false);
        interiorLights?.ForEach(x => x.enabled = false);

        //get wheel colliders
        var wheelColliders = transform.GetComponentsInChildren<WheelCollider>().ToList();
        foreach (var col in wheelColliders)
        {
            var effect = Instantiate(SimulatorManager.Instance.EnvironmentEffectsManager.TireSprayPrefab,
                col.transform.position + new Vector3(0f, -col.radius, 0f),
                Quaternion.identity).GetComponent<VisualEffect>();
            effect.transform.SetParent(transform);
            TireSprayDatas.Add(new TireSprayData {wheelCollider = col, visualEffect = effect});
        }
        SetTireSpraySpawnRate(0.0f, true);
    }

    private void CreateCinematicTransforms()
    {
        var bound = Controller.Bounds;
        var cinematicT = new GameObject("CenterFront").transform;
        cinematicT.position = new Vector3(bound.center.x, bound.min.y + 1f, bound.center.z + bound.max.z * 2);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center);
        Controller.CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("CenterTop").transform;
        cinematicT.position = new Vector3(bound.center.x, bound.max.y * 10f, bound.center.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center);
        Controller.CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("RightFront").transform;
        cinematicT.position = new Vector3(bound.center.x + bound.max.x + 1f, bound.min.y + 0.5f, bound.center.z + bound.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center + new Vector3(0f, 0.25f, 0f));
        Controller.CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("LeftFront").transform;
        cinematicT.position = new Vector3(bound.center.x - bound.max.x - 1f, bound.min.y + 0.5f, bound.center.z + bound.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center + new Vector3(0f, 0.25f, 0f));
        Controller.CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("RightBack").transform;
        cinematicT.position = new Vector3(bound.center.x + bound.max.x + 1f, bound.min.y + 0.5f, bound.center.z - bound.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center + new Vector3(0f, 0.25f, 0f));
        Controller.CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("LeftBack").transform;
        cinematicT.position = new Vector3(bound.center.x - bound.max.x - 1f, bound.min.y + 0.5f, bound.center.z - bound.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(bound.center + new Vector3(0f, 0.25f, 0f));
        Controller.CinematicCameraTransforms.Add(cinematicT);
    }

    private void CreateDriverViewTransform()
    {
        if (Controller.DriverViewTransform != null)
        {
            return;
        }

        var bounds = Controller.Bounds;
        var origin = new Vector3(bounds.center.x, bounds.max.y * 2f, bounds.center.z + bounds.max.z / 4f);
        var direction = Vector3.down * 10f;

        var view = new GameObject("DriverView").transform;
        view.position = origin;
        view.rotation = Quaternion.identity;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f, LayerMask.GetMask("Agent")))
        {
            view.position = hit.point;
        }

        view.SetParent(transform, true);
        Controller.DriverViewTransform = view;
    }

    public void IncrementHeadLightState()
    {
        CurrentHeadLightState = (int)CurrentHeadLightState == System.Enum.GetValues(typeof(HeadLightState)).Length - 1 ? HeadLightState.OFF : CurrentHeadLightState + 1;
    }

    private void StartIndicatorLeftStatus()
    {
        if (indicatorLeftIE != null)
        {
            StopCoroutine(indicatorLeftIE);
        }

        indicatorLeftIE = RunIndicatorLeftStatus();
        StartCoroutine(indicatorLeftIE);
    }

    private IEnumerator RunIndicatorLeftStatus()
    {
        while (LeftTurnSignal)
        {
            SetIndicatorLeftLights(true);
            yield return new WaitForSeconds(0.5f);
            SetIndicatorLeftLights(false);
            yield return new WaitForSeconds(0.5f);
        }

        SetIndicatorLeftLights(false);
    }

    private void SetIndicatorLeftLights(bool state)
    {
        indicatorLeftLights.ForEach(x => x.enabled = state);
        indicatorLeftLights.ForEach(x => x.intensity = state ? 200f : 0f);
        if (indicatorLeftLightRenderer != null)
        {
            indicatorLeftLightRenderer.material.SetFloat("_EmitIntensity", state ? 6f : 0f);
        }
    }
    
    private void StartIndicatorRightStatus()
    {
        if (indicatorRightIE != null)
        {
            StopCoroutine(indicatorRightIE);
        }

        indicatorRightIE = RunIndicatorRightStatus();
        StartCoroutine(indicatorRightIE);
    }

    private IEnumerator RunIndicatorRightStatus()
    {
        while (RightTurnSignal)
        {
            SetIndicatorRightLights(true);
            yield return new WaitForSeconds(0.5f);
            SetIndicatorRightLights(false);
            yield return new WaitForSeconds(0.5f);
        }

        SetIndicatorRightLights(false);
    }

    private void SetIndicatorRightLights(bool state)
    {
        indicatorRightLights.ForEach(x => x.enabled = state);
        indicatorRightLights.ForEach(x => x.intensity = state ? 200f : 0f);
        if (indicatorRightLightRenderer != null)
        {
            indicatorRightLightRenderer.material.SetFloat("_EmitIntensity", state ? 6f : 0f);
        }
    }

    private void StartIndicatorHazardStatus()
    {
        if (indicatorHazardIE != null)
        {
            StopCoroutine(indicatorHazardIE);
        }

        indicatorHazardIE = RunIndicatorHazardStatus();
        StartCoroutine(indicatorHazardIE);
    }
    
    private IEnumerator RunIndicatorHazardStatus()
    {
        while (HazardLights)
        {
            SetIndicatorHazardLights(true);
            yield return new WaitForSeconds(0.5f);
            SetIndicatorHazardLights(false);
            yield return new WaitForSeconds(0.5f);
        }

        SetIndicatorHazardLights(false);
    }

    private void SetIndicatorHazardLights(bool state)
    {
        indicatorLeftLights.ForEach(x => x.enabled = state);
        indicatorLeftLights.ForEach(x => x.intensity = state ? 200f : 0f);
        if (indicatorLeftLightRenderer != null)
        {
            indicatorLeftLightRenderer.material.SetFloat("_EmitIntensity", state ? 6f : 0f);
        }

        indicatorRightLights.ForEach(x => x.enabled = state);
        indicatorRightLights.ForEach(x => x.intensity = state ? 200f : 0f);
        if (indicatorRightLightRenderer != null)
        {
            indicatorRightLightRenderer.material.SetFloat("_EmitIntensity", state ? 6f : 0f);
        }
    }

    public void IncrementWiperState()
    {
        CurrentWiperState = (int)CurrentWiperState == System.Enum.GetValues(typeof(WiperState)).Length - 1 ? WiperState.OFF : CurrentWiperState + 1;
    }
    
#region network
    /// <summary>
    /// Broadcasts current vehicle actions state snapshot
    /// </summary>
    private void BroadcastStateSnapshot()
    {
        if (!isInitialized || !Loader.Instance.Network.IsMaster)
            return;

        var message = MessagesPool.Instance.GetMessage();
        message.AddressKey = Key;
        message.Content.PushFloat(tireSpraySpawnRate);
        message.Content.PushBool(_interiorLights);
        message.Content.PushBool(_reverseLights);
        message.Content.PushBool(_fogLights);
        message.Content.PushBool(_brakeLights);
        message.Content.PushBool(_hazardLights);
        message.Content.PushBool(_rightTurnSignal);
        message.Content.PushBool(_leftTurnSignal);
        message.Content.PushEnum<WiperState>((int)_currentWiperState);
        message.Content.PushEnum<HeadLightState>((int)_currentHeadLightState);
        message.Content.PushEnum<VehicleActionsPropertyName>((int)VehicleActionsPropertyName.StateSnapshot);
        message.Type = DistributedMessageType.ReliableOrdered;
        ((IMessageSender) this).BroadcastMessage(message);
    }

    /// <inheritdoc/>
    void IMessageReceiver.ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        //Ignore messages if this component is marked as destroyed
        if (this == null)
        {
            return;
        }

        var propertyName = distributedMessage.Content.PopEnum<VehicleActionsPropertyName>();
        switch (propertyName)
        {
            case VehicleActionsPropertyName.StateSnapshot:
                CurrentHeadLightState = distributedMessage.Content.PopEnum<HeadLightState>();
                CurrentWiperState = distributedMessage.Content.PopEnum<WiperState>();
                LeftTurnSignal = distributedMessage.Content.PopBool();
                RightTurnSignal = distributedMessage.Content.PopBool();
                HazardLights = distributedMessage.Content.PopBool();
                BrakeLights = distributedMessage.Content.PopBool();
                FogLights = distributedMessage.Content.PopBool();
                ReverseLights = distributedMessage.Content.PopBool();
                InteriorLight = distributedMessage.Content.PopBool();
                SetTireSpraySpawnRate(distributedMessage.Content.PopFloat());
                break;
            case VehicleActionsPropertyName.CurrentHeadLightState:
                CurrentHeadLightState = distributedMessage.Content.PopEnum<HeadLightState>();
                break;
            case VehicleActionsPropertyName.CurrentWiperState:
                CurrentWiperState = distributedMessage.Content.PopEnum<WiperState>();
                break;
            case VehicleActionsPropertyName.LeftTurnSignal:
                LeftTurnSignal = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.RightTurnSignal:
                RightTurnSignal = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.HazardLights:
                HazardLights = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.BrakeLights:
                BrakeLights = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.FogLights:
                FogLights = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.ReverseLights:
                ReverseLights = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.InteriorLight:
                InteriorLight = distributedMessage.Content.PopBool();
                break;
            case VehicleActionsPropertyName.TireSpraySpawnRate:
                SetTireSpraySpawnRate(distributedMessage.Content.PopFloat());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (Key != null)
        {
            messagesManager?.UnicastMessage(endPoint, distributedMessage);
        }
    }

    /// <inheritdoc/>
    void IMessageSender.BroadcastMessage(DistributedMessage distributedMessage)
    {
        if (Key != null)
        {
            messagesManager?.BroadcastMessage(distributedMessage);
        }
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    private void BroadcastProperty(VehicleActionsPropertyName propertyName, bool value)
    {
        if (!isInitialized || !Loader.Instance.Network.IsMaster)
            return;

        var message = MessagesPool.Instance.GetMessage(5);
        message.AddressKey = Key;
        message.Content.PushBool(value);
        message.Content.PushEnum<VehicleActionsPropertyName>((int)propertyName);
        message.Type = DistributedMessageType.ReliableOrdered;
        ((IMessageSender) this).BroadcastMessage(message);
    }

    private void BroadcastProperty(VehicleActionsPropertyName propertyName, float value)
    {
        if (!isInitialized || !Loader.Instance.Network.IsMaster)
            return;
        
        var message = MessagesPool.Instance.GetMessage(5);
        message.AddressKey = Key;
        message.Content.PushFloat(value);
        message.Content.PushEnum<VehicleActionsPropertyName>((int)propertyName);
        message.Type = DistributedMessageType.ReliableOrdered;
        ((IMessageSender) this).BroadcastMessage(message);
    }

    private enum VehicleActionsPropertyName
    {
        StateSnapshot = 0,
        CurrentHeadLightState = 1,
        CurrentWiperState = 2,
        LeftTurnSignal = 3,
        RightTurnSignal = 4,
        HazardLights = 5,
        BrakeLights = 6,
        FogLights = 7,
        ReverseLights = 8,
        InteriorLight = 9,
        TireSpraySpawnRate = 10
    }
#endregion
}
