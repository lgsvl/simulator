/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
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

public class VehicleActions : MonoBehaviour, IMessageSender, IMessageReceiver
{
    public Texture lowCookie;
    public Texture highCookie;

    private AgentController agentController;
    private Rigidbody RB;

    [HideInInspector]
    public Bounds Bounds;
    [HideInInspector]
    public List<Transform> CinematicCameraTransforms = new List<Transform>();

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
    private List<TireSprayData> TireSprayDatas = new List<TireSprayData>();
    private WheelHit WheelColliderHit;
    private Quaternion WheelColliderRot;
    private EnvironmentEffectsManager EnviroManager;

    //Network
    private MessagesManager messagesManager;
    private string key;
    public string Key => key ?? (key = $"{HierarchyUtilities.GetPath(transform)}VehicleActions");
    
    public enum HeadLightState { OFF = 0, LOW = 1, HIGH = 2 };
    private HeadLightState _currentHeadLightState = HeadLightState.OFF;
    public HeadLightState CurrentHeadLightState
    {
        get => _currentHeadLightState;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _currentHeadLightState = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    headLights.ForEach(x => x.enabled = false);
                    headLights.ForEach(x => x.intensity = 0f);
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 0);
                    }
                    break;
                case HeadLightState.LOW:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 250f);
                    if (lowCookie != null)
                    {
                        headLights.ForEach(x => x.cookie = lowCookie);
                    }
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 4);
                    }
                    break;
                case HeadLightState.HIGH:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 500f);
                    if (highCookie != null)
                    {
                        headLights.ForEach(x => x.cookie = highCookie);
                    }
                    if (headLightRenderer != null)
                    {
                        headLightRenderer.material.SetFloat("_EmitIntensity", 12);
                    }
                    break;
            }

            if (Loader.Instance.Network.IsMaster)
            {
                var message = MessagesPool.Instance.GetMessage(8);
                message.AddressKey = Key;
                message.Content.PushEnum<HeadLightState>((int)value);
                message.Content.PushEnum<VehicleActionsPropertyName>((int)VehicleActionsPropertyName.CurrentHeadLightState);
                message.Type = DistributedMessageType.ReliableOrdered;
                BroadcastMessage(message);
            }
        }
    }

    public enum WiperState { OFF = 0, LOW = 1, MED = 2, HIGH = 3 };
    private WiperState _currentWiperState = WiperState.OFF;
    public WiperState CurrentWiperState
    {
        get => _currentWiperState;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _currentWiperState = value;

            if (Loader.Instance.Network.IsMaster)
            {
                var message = MessagesPool.Instance.GetMessage(8);
                message.AddressKey = Key;
                message.Content.PushEnum<WiperState>((int)value);
                message.Content.PushEnum<VehicleActionsPropertyName>((int)VehicleActionsPropertyName.CurrentWiperState);
                message.Type = DistributedMessageType.ReliableOrdered;
                BroadcastMessage(message);
            }
        }
    }

    private bool _leftTurnSignal = false;
    public bool LeftTurnSignal
    {
        get => _leftTurnSignal;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _leftTurnSignal = value;
            _rightTurnSignal = _hazardLights = false;
            StartIndicatorLeftStatus();
            
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.LeftTurnSignal, value);
        }
    }

    private bool _rightTurnSignal = false;
    public bool RightTurnSignal
    {
        get => _rightTurnSignal;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _rightTurnSignal = value;
            _leftTurnSignal = _hazardLights = false;
            StartIndicatorRightStatus();
            
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.RightTurnSignal, value);
        }
    }

    private bool _hazardLights = false;
    public bool HazardLights
    {
        get => _hazardLights;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _hazardLights = value;
            _leftTurnSignal = _rightTurnSignal = false;
            StartIndicatorHazardStatus();
            
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.HazardLights, value);
        }
    }

    private bool _brakeLights = false;
    public bool BrakeLights
    {
        get => _brakeLights;
        set
        {
            _brakeLights = value;
            brakeLights.ForEach(x => x.enabled = _brakeLights);
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    if (brakeLightRenderer != null)
                    {
                        brakeLightRenderer.material.SetFloat("_EmitIntensity", _brakeLights ? 4 : 0);
                    }
                    break;
                case HeadLightState.LOW:
                case HeadLightState.HIGH:
                    if (brakeLightRenderer != null)
                    {
                        brakeLightRenderer.material.SetFloat("_EmitIntensity", _brakeLights ? 4 : 1.1f);
                    }
                    break;
            }
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.BrakeLights, value);
        }
    }

    private bool _fogLights = false;
    public bool FogLights
    {
        get => _fogLights;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _fogLights = value;
            fogLights.ForEach(x => x.enabled = _fogLights);
            if (fogLightRenderer != null)
            {
                fogLightRenderer.material.SetFloat("_EmitIntensity", 2);
            }
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.FogLights, value);
        }
    }

    private bool _reverseLights = false;
    public bool ReverseLights
    {
        get => _reverseLights;
        set
        {
            _reverseLights = value;
            indicatorReverseLights.ForEach(x => x.enabled = _reverseLights);
            if (indicatorReverseLightRenderer != null)
            {
                indicatorReverseLightRenderer.material.SetFloat("_EmitIntensity", _reverseLights ? 2 : 0);
            }
            if (Loader.Instance.Network.IsMaster)
                BroadcastProperty(VehicleActionsPropertyName.ReverseLights, value);
        }
    }

    private bool _interiorLights = false;
    public bool InteriorLight
    {
        get => _interiorLights;
        set
        {
            if (!agentController.Active && !Loader.Instance.Network.IsClient)
                return;

            _interiorLights = value;
            interiorLights.ForEach(x => x.enabled = _interiorLights);
            
            if (Loader.Instance.Network.IsMaster)
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
        messagesManager = Loader.Instance.Network.MessagesManager;
        messagesManager?.RegisterObject(this);
        RB = GetComponent<Rigidbody>();
        EnviroManager = SimulatorManager.Instance.EnvironmentEffectsManager;
    }

    private void Update()
    {
        foreach (var item in TireSprayDatas)
        {
            if (item.wheelCollider.GetGroundHit(out WheelColliderHit))
            {
                item.visualEffect.transform.position = WheelColliderHit.point;
                item.wheelCollider.GetWorldPose(out var pos, out WheelColliderRot);
                item.visualEffect.transform.rotation = WheelColliderRot;
                if (EnviroManager != null)
                {
                    if (RB != null)
                    {
                        var localVel = transform.InverseTransformDirection(RB.velocity);
                        var lerp = 0f;

                        if (EnviroManager.Wet <= 0.25)
                        {
                            item.visualEffect.SetFloat("_SpawnRate", 0);
                        }
                        else if (EnviroManager.Wet > 0.25 && EnviroManager.Wet <= 0.5)
                        {
                            lerp = localVel.z > 0 ? Mathf.InverseLerp(0, 100, localVel.z) : 0f;
                            item.visualEffect.SetFloat("_SpawnRate", lerp);
                        }
                        else if (EnviroManager.Wet > 0.5 && EnviroManager.Wet <= 0.75)
                        {
                            lerp = localVel.z > 0 ? Mathf.InverseLerp(0, 75, localVel.z) : 0f;
                            item.visualEffect.SetFloat("_SpawnRate", lerp);
                        }
                        else if (EnviroManager.Wet > 0.75 && EnviroManager.Wet <= 1)
                        {
                            lerp = localVel.z > 0 ? Mathf.InverseLerp(0, 25, localVel.z) : 0f;
                            item.visualEffect.SetFloat("_SpawnRate", lerp);
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        messagesManager?.UnregisterObject(this);
    }

    private void SetNeededComponents()
    {
        var dynamics = GetComponent<VehicleSMI>();
        agentController = GetComponent<AgentController>();
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var animators = GetComponentsInChildren<Animator>(true); // TODO wipers doors windows

        Bounds = new Bounds(transform.position, Vector3.zero);
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
            Bounds.Encapsulate(child.bounds);
        }

        CreateCinematicTransforms();

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, Bounds.size.y / 2, gtBoxCollider.center.z);
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
        brakeLights?.ForEach(x => x.enabled = false);
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
            TireSprayDatas.Add(new TireSprayData { wheelCollider = col, visualEffect = effect });
        }
    }

    private void CreateCinematicTransforms()
    {
        var cinematicT = new GameObject("CenterFront").transform;
        cinematicT.position = new Vector3(Bounds.center.x, Bounds.min.y + 1f, Bounds.center.z + Bounds.max.z * 2);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center);
        CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("CenterTop").transform;
        cinematicT.position = new Vector3(Bounds.center.x, Bounds.max.y * 10f, Bounds.center.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center);
        CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("RightFront").transform;
        cinematicT.position = new Vector3(Bounds.center.x + Bounds.max.x + 1f, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center + new Vector3(0f, 0.25f, 0f));
        CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("LeftFront").transform;
        cinematicT.position = new Vector3(Bounds.center.x - Bounds.max.x - 1f, Bounds.min.y + 0.5f, Bounds.center.z + Bounds.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center + new Vector3(0f, 0.25f, 0f));
        CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("RightBack").transform;
        cinematicT.position = new Vector3(Bounds.center.x + Bounds.max.x + 1f, Bounds.min.y + 0.5f, Bounds.center.z - Bounds.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center + new Vector3(0f, 0.25f, 0f));
        CinematicCameraTransforms.Add(cinematicT);
        cinematicT = new GameObject("LeftBack").transform;
        cinematicT.position = new Vector3(Bounds.center.x - Bounds.max.x - 1f, Bounds.min.y + 0.5f, Bounds.center.z - Bounds.max.z);
        cinematicT.SetParent(transform, true);
        cinematicT.LookAt(Bounds.center + new Vector3(0f, 0.25f, 0f));
        CinematicCameraTransforms.Add(cinematicT);
    }

    public void IncrementHeadLightState()
    {
        CurrentHeadLightState = (int)CurrentHeadLightState == System.Enum.GetValues(typeof(HeadLightState)).Length - 1 ? HeadLightState.OFF : CurrentHeadLightState + 1;
    }
    
    private void StartIndicatorLeftStatus()
    {
        if (indicatorLeftIE != null)
            StopCoroutine(indicatorLeftIE);
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
        indicatorLeftLightRenderer.material.SetFloat("_EmitIntensity", state ? 2 : 0);
        indicatorLeftLights.ForEach(x => x.enabled = state);
    }
    
    private void StartIndicatorRightStatus()
    {
        if (indicatorRightIE != null)
            StopCoroutine(indicatorRightIE);
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
        indicatorRightLightRenderer.material.SetFloat("_EmitIntensity", state ? 2 : 0);
        indicatorRightLights.ForEach(x => x.enabled = state);
    }

    private void StartIndicatorHazardStatus()
    {
        if (indicatorHazardIE != null)
            StopCoroutine(indicatorHazardIE);
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
        indicatorLeftLightRenderer.material.SetFloat("_EmitIntensity", state ? 2 : 0);
        indicatorLeftLights.ForEach(x => x.enabled = state);
        indicatorRightLightRenderer.material.SetFloat("_EmitIntensity", state ? 2 : 0);
        indicatorRightLights.ForEach(x => x.enabled = state);
    }

    public void IncrementWiperState()
    {
        CurrentWiperState = (int)CurrentWiperState == System.Enum.GetValues(typeof(WiperState)).Length - 1 ? WiperState.OFF : CurrentWiperState + 1;
    }
    
    /// <inheritdoc/>
    public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
    {
        //Ignore messages if this component is marked as destroyed
        if (this == null)
            return;
        
        var propertyName = distributedMessage.Content.PopEnum<VehicleActionsPropertyName>();
        switch (propertyName)
        {
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc/>
    public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
    {
        if (Key != null)
            messagesManager?.UnicastMessage(endPoint, distributedMessage);
    }

    /// <inheritdoc/>
    public void BroadcastMessage(DistributedMessage distributedMessage)
    {
        if (Key != null)
            messagesManager?.BroadcastMessage(distributedMessage);
    }

    /// <inheritdoc/>
    void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
    {
        //TODO support reconnection - send instantiation messages to the peer
    }

    private void BroadcastProperty(VehicleActionsPropertyName propertyName, bool value)
    {
        var message = MessagesPool.Instance.GetMessage(5);
        message.AddressKey = Key;
        message.Content.PushBool(value);
        message.Content.PushEnum<VehicleActionsPropertyName>((int)propertyName);
        message.Type = DistributedMessageType.ReliableOrdered;
        BroadcastMessage(message);
    }

    private enum VehicleActionsPropertyName
    {
        CurrentHeadLightState = 0,
        CurrentWiperState = 1,
        LeftTurnSignal = 2,
        RightTurnSignal = 3,
        HazardLights = 4,
        BrakeLights = 5,
        FogLights = 6,
        ReverseLights = 7,
        InteriorLight = 8,
    }
}
