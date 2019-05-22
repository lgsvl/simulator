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
    
    private enum HeadLightState { OFF = 0, LOW = 1, HIGH = 2 };
    private HeadLightState currentHeadLightState = HeadLightState.OFF;
    //private bool isBrake = false;
    public bool isIndicatorLeft { get; set; } = false;
    public bool isIndicatorRight { get; set; } = false;
    private bool isHazard = false;
    private IEnumerator indicatorLeftIE;
    private IEnumerator indicatorRightIE;
    private IEnumerator indicatorHazardIE;
    private bool isReverse = false;
    private bool isFog = false;
    //private bool isInterior = false;

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

    public void SetHeadLights(int state = -1)
    {
        if (state == -1)
            currentHeadLightState = (int)currentHeadLightState == System.Enum.GetValues(typeof(HeadLightState)).Length - 1 ? HeadLightState.OFF : currentHeadLightState + 1;
        else
            currentHeadLightState = (HeadLightState)state;
        switch (currentHeadLightState)
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
            default:
                Debug.LogError("Invalid light state");
                break;
        }
        headLightRenderer.material.SetColor("_EmissiveColor", currentHeadLightState == HeadLightState.OFF ? Color.black : Color.white);
    }
    
    public void SetBrakeLights(bool state)
    {
        switch (currentHeadLightState)
        {
            case HeadLightState.OFF:
                brakeLightRenderer.material.SetColor("_EmissiveColor", state ? Color.red : Color.black);
                break;
            case HeadLightState.LOW:
            case HeadLightState.HIGH:
                brakeLightRenderer.material.SetColor("_EmissiveColor", state ? Color.red : new Color(0.5f, 0f, 0f));
                break;
        }
        brakeLights.ForEach(x => x.enabled = state);
    }

    public void ToggleIndicatorLeftLights(bool isForced = false, bool forcedState = false)
    {
        isIndicatorLeft = isForced ? forcedState : !isIndicatorLeft;
        if (isIndicatorLeft)
        {
            isIndicatorRight = isHazard = false;
            StartIndicatorLeftStatus();
        }
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
        while (isIndicatorLeft)
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

    public void ToggleIndicatorRightLights(bool isForced = false, bool forcedState = false)
    {
        isIndicatorRight = isForced ? forcedState : !isIndicatorRight;
        if (isIndicatorRight)
        {
            isIndicatorLeft = isHazard = false;
            StartIndicatorRightStatus();
        }
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
        while (isIndicatorRight)
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

    public void ToggleIndicatorHazardLights(bool isForced = false, bool forcedState = false)
    {
        isHazard = isForced ? forcedState : !isHazard;
        if (isHazard)
        {
            isIndicatorLeft = isIndicatorRight = false;
            StartIndicatorHazardStatus();
        }
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
        while (isHazard)
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

    public void SetIndicatorReverseLights(bool state)
    {
        if (isReverse == state) return;

        isReverse = state;
        indicatorReverseLightRenderer.material.SetColor("_EmissiveColor", state ? Color.white : Color.black);
        indicatorReverseLights.ForEach(x => x.enabled = state);
    }

    public void ToggleFogLights(bool isForced = false, bool forcedState = false)
    {
        isFog = isForced ? forcedState : !isFog;
        SetFogLights(isFog);
    }

    private void SetFogLights(bool state)
    {
        fogLightRenderer.material.SetColor("_EmissiveColor", state ? Color.white : Color.black);
        fogLights.ForEach(x => x.enabled = state);
    }

    public void ToggleInteriorLight(bool isForced = false, bool forcedState = false)
    {
        interiorLight.enabled = isForced ? forcedState : !interiorLight.enabled;
    }
}
