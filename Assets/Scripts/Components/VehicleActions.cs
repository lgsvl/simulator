/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleActions : MonoBehaviour
{
    private AgentController agentController;

    public Bounds Bounds;

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
    private Light interiorLight;
    
    public enum HeadLightState { OFF = 0, LOW = 1, HIGH = 2 };
    private HeadLightState _currentHeadLightState = HeadLightState.OFF;
    public HeadLightState CurrentHeadLightState
    {
        get => _currentHeadLightState;
        set
        {
            if (!agentController.Active)
                return;

            _currentHeadLightState = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    headLights.ForEach(x => x.enabled = false);
                    headLightRenderer?.material.SetVector("_EmissiveColor", Color.black);
                    break;
                case HeadLightState.LOW:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 25f);
                    headLightRenderer?.material.SetVector("_EmissiveColor", Color.white * 200);
                    break;
                case HeadLightState.HIGH:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 50f);
                    headLightRenderer?.material.SetVector("_EmissiveColor", Color.white * 300);
                    break;
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
            if (!agentController.Active)
                return;

            _currentWiperState = value;
            // animation
            // ui event
        }
    }

    private bool _leftTurnSignal = false;
    public bool LeftTurnSignal
    {
        get => _leftTurnSignal;
        set
        {
            if (!agentController.Active)
                return;

            _leftTurnSignal = value;
            _rightTurnSignal = _hazardLights = false;
            StartIndicatorLeftStatus();
        }
    }

    private bool _rightTurnSignal = false;
    public bool RightTurnSignal
    {
        get => _rightTurnSignal;
        set
        {
            if (!agentController.Active)
                return;

            _rightTurnSignal = value;
            _leftTurnSignal = _hazardLights = false;
            StartIndicatorRightStatus();
        }
    }

    private bool _hazardLights = false;
    public bool HazardLights
    {
        get => _hazardLights;
        set
        {
            if (!agentController.Active)
                return;

            _hazardLights = value;
            _leftTurnSignal = _rightTurnSignal = false;
            StartIndicatorHazardStatus();
        }
    }

    private bool _brakeLights = false;
    public bool BrakeLights
    {
        get => _brakeLights;
        set
        {
            _brakeLights = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    brakeLightRenderer?.material.SetVector("_EmissiveColor", _brakeLights ? Color.red * 50 : Color.black);
                    break;
                case HeadLightState.LOW:
                case HeadLightState.HIGH:
                    brakeLightRenderer?.material.SetVector("_EmissiveColor", _brakeLights ? Color.red * 50 : new Color(0.5f, 0f, 0f) * 10);
                    break;
            }
            brakeLights.ForEach(x => x.enabled = _brakeLights);
        }
    }

    private bool _fogLights = false;
    public bool FogLights
    {
        get => _fogLights;
        set
        {
            if (!agentController.Active)
                return;

            _fogLights = value;
            fogLightRenderer?.material.SetVector("_EmissiveColor", _fogLights ? Color.white * 200 : Color.black);
            fogLights.ForEach(x => x.enabled = _fogLights);
        }
    }

    private bool _reverseLights = false;
    public bool ReverseLights
    {
        get => _reverseLights;
        set
        {
            _reverseLights = value;
            indicatorReverseLightRenderer?.material.SetVector("_EmissiveColor", _reverseLights ? Color.white * 10 : Color.black);
            indicatorReverseLights.ForEach(x => x.enabled = _reverseLights);
        }
    }

    private bool _interiorLight = false;
    public bool InteriorLight
    {
        get => _interiorLight;
        set
        {
            if (!agentController.Active)
                return;

            _interiorLight = value;
            interiorLight.enabled = _interiorLight;
        }
    }

    private IEnumerator indicatorLeftIE;
    private IEnumerator indicatorRightIE;
    private IEnumerator indicatorHazardIE;

    private void Awake()
    {
        SetNeededComponents();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void SetNeededComponents()
    {
        agentController = GetComponent<AgentController>();
        var allRenderers = GetComponentsInChildren<Renderer>();
        var animators = GetComponentsInChildren<Animator>(); // TODO wipers doors windows

        Bounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer child in allRenderers)
        {
            if (child.name == "HeadLights")
                headLightRenderer = child;
            if (child.name == "BrakeLights")
                brakeLightRenderer = child;
            if (child.name == "LeftTurnIndicator")
                indicatorLeftLightRenderer = child;
            if (child.name == "RightTurnIndicator")
                indicatorRightLightRenderer = child;
            if (child.name == "ReverseIndicator")
                indicatorReverseLightRenderer = child;
            if (child.name == "FogLights")
                fogLightRenderer = child;
            Bounds.Encapsulate(child.bounds);
        }

        // GroundTruth Box Collider
        var gtBox = new GameObject("GroundTruthBox");
        var gtBoxCollider = gtBox.AddComponent<BoxCollider>();
        gtBoxCollider.isTrigger = true;
        gtBoxCollider.size = Bounds.size;
        gtBoxCollider.center = new Vector3(gtBoxCollider.center.x, Bounds.size.y / 2, gtBoxCollider.center.z);
        gtBox.transform.parent = transform;
        gtBox.layer = LayerMask.NameToLayer("GroundTruth");
        
        headLightRenderer?.material.SetColor("_EmissiveColor", Color.black);
        brakeLightRenderer?.material.SetColor("_EmissiveColor", Color.black);
        indicatorLeftLightRenderer?.material.SetColor("_EmissiveColor", Color.black);
        indicatorRightLightRenderer?.material.SetColor("_EmissiveColor", Color.black);
        indicatorReverseLightRenderer?.material.SetColor("_EmissiveColor", Color.black);
        fogLightRenderer?.material.SetColor("_EmissiveColor", Color.black);

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
            if (t.name == "InteriorLight")
                interiorLight = t.GetComponent<Light>();
        }

        headLights?.ForEach(x => x.enabled = false);
        brakeLights?.ForEach(x => x.enabled = false);
        indicatorLeftLights?.ForEach(x => x.enabled = false);
        indicatorRightLights?.ForEach(x => x.enabled = false);
        indicatorReverseLights?.ForEach(x => x.enabled = false);
        fogLights?.ForEach(x => x.enabled = false);
        interiorLight.enabled = false;
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
        indicatorLeftLightRenderer.material.SetVector("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) * 10 : Color.black);
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
        indicatorRightLightRenderer.material.SetVector("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) * 10 : Color.black);
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
        indicatorLeftLightRenderer.material.SetVector("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) * 10 : Color.black);
        indicatorLeftLights.ForEach(x => x.enabled = state);
        indicatorRightLightRenderer.material.SetVector("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) * 10 : Color.black);
        indicatorRightLights.ForEach(x => x.enabled = state);
    }

    public void IncrementWiperState()
    {
        CurrentWiperState = (int)CurrentWiperState == System.Enum.GetValues(typeof(WiperState)).Length - 1 ? WiperState.OFF : CurrentWiperState + 1;
    }
}
