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
            _currentHeadLightState = value;
            switch (_currentHeadLightState)
            {
                case HeadLightState.OFF:
                    headLights.ForEach(x => x.enabled = false);
                    break;
                case HeadLightState.LOW:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 25f);
                    break;
                case HeadLightState.HIGH:
                    headLights.ForEach(x => x.enabled = true);
                    headLights.ForEach(x => x.intensity = 50f);
                    break;
            }
            headLightRenderer?.material.SetColor("_EmissiveColor", _currentHeadLightState == HeadLightState.OFF ? Color.black : Color.white);
        }
    }

    public enum WiperState { OFF = 0, LOW = 1, MED = 2, HIGH = 3 };
    private WiperState _currentWiperState = WiperState.OFF;
    public WiperState CurrentWiperState
    {
        get => _currentWiperState;
        set
        {
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
                    brakeLightRenderer?.material.SetColor("_EmissiveColor", _brakeLights ? Color.red : Color.black);
                    break;
                case HeadLightState.LOW:
                case HeadLightState.HIGH:
                    brakeLightRenderer?.material.SetColor("_EmissiveColor", _brakeLights ? Color.red : new Color(0.5f, 0f, 0f));
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
            _fogLights = value;
            fogLightRenderer?.material.SetColor("_EmissiveColor", _fogLights ? Color.white : Color.black);
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
            indicatorReverseLightRenderer?.material.SetColor("_EmissiveColor", _reverseLights ? Color.white : Color.black);
            indicatorReverseLights.ForEach(x => x.enabled = _reverseLights);
        }
    }

    private bool _interiorLight = false;
    public bool InteriorLight
    {
        get => _interiorLight;
        set
        {
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
        var allRenderers = GetComponentsInChildren<Renderer>();
        var animators = GetComponentsInChildren<Animator>(); // TODO wipers doors windows

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
        }
        
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
        indicatorLeftLightRenderer.material.SetColor("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) : Color.black);
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
        indicatorRightLightRenderer.material.SetColor("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) : Color.black);
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
        indicatorLeftLightRenderer.material.SetColor("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) : Color.black);
        indicatorLeftLights.ForEach(x => x.enabled = state);
        indicatorRightLightRenderer.material.SetColor("_EmissiveColor", state ? new Color(1f, 0.5f, 0f) : Color.black);
        indicatorRightLights.ForEach(x => x.enabled = state);
    }

    public void IncrementWiperState()
    {
        CurrentWiperState = (int)CurrentWiperState == System.Enum.GetValues(typeof(WiperState)).Length - 1 ? WiperState.OFF : CurrentWiperState + 1;
    }
}
