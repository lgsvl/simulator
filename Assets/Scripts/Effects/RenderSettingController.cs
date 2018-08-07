/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class RenderSettingController : DayNightEventListener
{
    private static bool usingGI = true;
    public Camera tiedCam;
    private void Awake()
    {
        if (tiedCam == null)
        {
            tiedCam = GetComponent<Camera>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (DayNightEventsController.IsInstantiated)
        {
            if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunrise)
                OnSunRise();
            if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Day)
                OnDay();
            if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunset)
                OnSunSet();
            if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Night)
                OnNight();
        }
    }
    protected override void OnSunRise()
    {
        tiedCam.farClipPlane = 3000.0f;
        if (usingGI)
        {
            RenderSettings.ambientIntensity = 0.3f; //With GI
        }
        else
        {
            RenderSettings.ambientIntensity = 0.35f; //Without GI
        }
        RenderSettings.reflectionIntensity = 0.7f;
        QualitySettings.shadowDistance = 500.0f;

    }
    protected override void OnDay()
    {
        tiedCam.farClipPlane = 3000.0f;
        if (usingGI)
        {
            RenderSettings.ambientIntensity = 0.5f; //With GI
        }
        else
        {
            RenderSettings.ambientIntensity = 0.7f; //Without GI
        }
        RenderSettings.reflectionIntensity = 1f;
        QualitySettings.shadowDistance = 500.0f;
    }
    protected override void OnSunSet()
    {
        tiedCam.farClipPlane = 3000.0f;
        if (usingGI)
        {
            RenderSettings.ambientIntensity = 0.3f; //With GI
        }
        else
        {
            RenderSettings.ambientIntensity = 0.35f; //Without GI
        }
        RenderSettings.reflectionIntensity = 0.7f;
        QualitySettings.shadowDistance = 500.0f;
    }
    protected override void OnNight()
    {
        tiedCam.farClipPlane = 2000.0f;
        RenderSettings.ambientIntensity = 0.0f;
        RenderSettings.reflectionIntensity = 0.0f;
        QualitySettings.shadowDistance = 150.0f; //Reduce shadow distance at night
    }
}
