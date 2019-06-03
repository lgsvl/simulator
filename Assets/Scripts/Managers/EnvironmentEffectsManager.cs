/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

public enum TimeOfDayStateTypes
{
    Day,
    Night,
    Sunrise,
    Sunset
};

public class EnvironmentEffectsManager : MonoBehaviour
{
    [System.Serializable]
    private struct LightParameters
    {
        public Color skyColor;
        public Color groundColor;
        public Color sunColor;
        public float sunIntensity;
        public float sunSize;
        public float sunSizeConvergence;
        public float atmoThickness;
        public float exposure;
        public float multiplier;
    }
    
    private LightParameters sunriseSky = new LightParameters
    {
        skyColor = new Color(0.2235f, 0.1803f, 0.4470f, 1f),
        groundColor = new Color(0.2058f, 0.1954f, 0.1756f, 1f),
        sunColor = new Color(1, 0.3517f, 0f, 1f),
        sunIntensity = 3.141593f,
        sunSize = 0.025f,
        sunSizeConvergence = 2f,
        atmoThickness = 0.65f,
        exposure = 1.26f,
        multiplier = 1f
    };

    private LightParameters daySky = new LightParameters
    {
        skyColor = new Color(0.3450f, 0.4f, 0.6504f, 1f),
        groundColor = new Color(0.5294f, 0.5215f, 0.5647f, 1f),
        sunColor = new Color(0.9852f, 0.9513f, 0.8403f, 1f),
        sunIntensity = 3.141593f,
        sunSize = 0.025f,
        sunSizeConvergence = 10f,
        atmoThickness = 0.65f,
        exposure = 1.26f,
        multiplier = 1f
    };

    private LightParameters sunsetSky = new LightParameters
    {
        skyColor = new Color(0.2235f, 0.1803f, 0.4470f, 1f),
        groundColor = new Color(0.2058f, 0.1954f, 0.1756f, 1f),
        sunColor = new Color(1, 0.3517f, 0f, 1f),
        sunIntensity = 3.141593f,
        sunSize = 0.025f,
        sunSizeConvergence = 2f,
        atmoThickness = 0.65f,
        exposure = 1.26f,
        multiplier = 1f
    };

    private LightParameters nightSky = new LightParameters
    {
        skyColor = new Color(0.3137f, 0.3411f, 0.3882f, 1f),
        groundColor = new Color(0.2745f, 0.2823f, 0.3098f, 1f),
        sunColor = new Color(0.4056f, 0.4056f, 0.4056f, 1f),
        sunIntensity = 0.5f,
        sunSize = 0.025f,
        sunSizeConvergence = 10f,
        atmoThickness = 0.11f,
        exposure = 0.52f,
        multiplier = 0.5f
    };

    public enum TimeOfDayCycleTypes
    {
        Freeze,
        Normal,
        Double,
        Quadruple
    };
    
    [Space(5, order = 0)]
    [Header("TimeOfDay", order = 1)]
    public float currentTimeOfDay = 12f;
    public TimeOfDayCycleTypes currentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    public TimeOfDayStateTypes currentTimeOfDayState { get; private set; } = TimeOfDayStateTypes.Day;
    public event Action<TimeOfDayStateTypes> TimeOfDayChanged;
    public GameObject sunGO;
    private Light sun;
    private Volume volume;
    private ProceduralSky skyVolume;
    private LightParameters fromLightParam = new LightParameters();
    private LightParameters toLightParam = new LightParameters();
    private LightParameters currentLightParam = new LightParameters();
    private float cycleDurationSeconds = 360f;
    private float sunRiseBegin = 6.0f;
    private float sunRiseEnd = 7.0f;
    private float sunSetBegin = 17.0f;
    private float sunSetEnd = 18.0f;
    private float fromTimeOfDay;
    private float toTimeOfDay;
    public DateTime dateTime; // TODO private once bug fixed in backend
    private List<TimeOfDayLight> timeOfDayLights = new List<TimeOfDayLight>();

    [Space(5, order = 0)]
    [Header("Rain", order = 1)]
    public ParticleSystem rainPfx;
    [Range(0f, 1f)]
    public float rain = 0f;
    private float prevRain = 0f;
    private List<RainVolume> rainVolumes = new List<RainVolume>();
    private List<ParticleSystem> rainPfxs = new List<ParticleSystem>();

    [Space(5, order = 0)]
    [Header("Fog", order = 1)]
    [Range(0f, 1f)]
    public float fog = 0f;
    private float prevFog = 0f;
    private ExponentialFog fogVolume;

    [Space(5, order = 0)]
    [Header("Cloud", order = 1)]
    [Range(0f, 1f)]
    public float cloud = 0f;

    [Space(5, order = 0)]
    [Header("Wet", order = 1)]
    [Range(0f, 1f)]
    public float wet = 0f;

    private void Start()
    {
        InitEnvironmentEffects();
    }

    private void Update()
    {
        TimeOfDayCycle();
        UpdateRain();
        UpdateFog();
    }
    
    private void InitEnvironmentEffects()
    {
        if (SimulatorManager.Instance.Config != null)
        {
            fog = SimulatorManager.Instance.Config.Fog;
            rain = SimulatorManager.Instance.Config.Rain;
            wet = SimulatorManager.Instance.Config.Wetness;
            cloud = SimulatorManager.Instance.Config.Cloudiness;
            dateTime = SimulatorManager.Instance.Config.TimeOfDay;
            //currentTimeOfDay = (float)SimulatorManager.Instance.Config.TimeOfDay.Hour + ((float)SimulatorManager.Instance.Config.TimeOfDay.Minute * 0.01f);
        }
        sunGO = Instantiate(sunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f));
        sun = sunGO.GetComponent<Light>(); // noon TODO real pos and rotation
        volume = FindObjectOfType<Volume>();
        volume.profile.TryGet<ProceduralSky>(out skyVolume);
        volume.profile.TryGet<ExponentialFog>(out fogVolume);
        rainVolumes.AddRange(FindObjectsOfType<RainVolume>());
        foreach (var volume in rainVolumes)
            rainPfxs.Add(volume.Init(rainPfx));
        timeOfDayLights.AddRange(FindObjectsOfType<TimeOfDayLight>());
        foreach (var light in timeOfDayLights)
            light.Init(currentTimeOfDayState);
        TimeOfDayCycle();
    }

    private void TimeOfDayCycle()
    {
        sun.transform.rotation = Quaternion.Euler((currentTimeOfDay / 24.0f) * 360.0f - 90.0f, 0, 0);
        switch (currentTimeOfDayCycle)
        {
            case TimeOfDayCycleTypes.Freeze:
                break;
            case TimeOfDayCycleTypes.Normal:
                currentTimeOfDay += (24f / cycleDurationSeconds) * Time.deltaTime;
                break;
            case TimeOfDayCycleTypes.Double:
                currentTimeOfDay += (24f / cycleDurationSeconds) * Time.deltaTime * 2;
                break;
            case TimeOfDayCycleTypes.Quadruple:
                currentTimeOfDay += (24f / cycleDurationSeconds) * Time.deltaTime * 4;
                break;
            default:
                break;
        }
        if (currentTimeOfDay >= 24) currentTimeOfDay = 0f;
        float morning = (sunRiseBegin + sunRiseEnd) / 2.0f;
        float evening = (sunSetBegin + sunSetEnd) / 2.0f;

        if (currentTimeOfDay < sunRiseBegin)
        {
            fromLightParam = nightSky;
            toLightParam = nightSky;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }
        else if (currentTimeOfDay < morning)
        {
            fromLightParam = nightSky;
            toLightParam = sunriseSky;
            fromTimeOfDay = sunRiseBegin;
            toTimeOfDay = morning;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (currentTimeOfDay < sunRiseEnd)
        {
            fromLightParam = sunriseSky;
            toLightParam = daySky;
            fromTimeOfDay = morning;
            toTimeOfDay = sunRiseEnd;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (currentTimeOfDay < sunSetBegin)
        {
            fromLightParam = daySky;
            toLightParam = daySky;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Day);
        }
        else if (currentTimeOfDay < evening)
        {
            fromLightParam = daySky;
            toLightParam = sunsetSky;
            fromTimeOfDay = sunSetBegin;
            toTimeOfDay = evening;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else if (currentTimeOfDay < sunSetEnd)
        {
            fromLightParam = sunsetSky;
            toLightParam = nightSky;
            fromTimeOfDay = evening;
            toTimeOfDay = sunSetEnd;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else
        {
            fromLightParam = nightSky;
            toLightParam = nightSky;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }

        TimeOfDayColorChange();
    }

    private void TimeOfDayColorChange()
    {
        var f = Mathf.InverseLerp(fromTimeOfDay, toTimeOfDay, currentTimeOfDay);
        currentLightParam = new LightParameters
        {
            skyColor = Color.Lerp(fromLightParam.skyColor, toLightParam.skyColor, f),
            groundColor = Color.Lerp(fromLightParam.groundColor, toLightParam.groundColor, f),
            sunColor = Color.Lerp(fromLightParam.sunColor, toLightParam.sunColor, f),
            sunIntensity = Mathf.Lerp(fromLightParam.sunIntensity, toLightParam.sunIntensity, f),
            sunSize = Mathf.Lerp(fromLightParam.sunSize, toLightParam.sunSize, f),
            sunSizeConvergence = Mathf.Lerp(fromLightParam.sunSizeConvergence, toLightParam.sunSizeConvergence, f),
            atmoThickness = Mathf.Lerp(fromLightParam.atmoThickness, toLightParam.atmoThickness, f),
            exposure = Mathf.Lerp(fromLightParam.exposure, toLightParam.exposure, f),
            multiplier = Mathf.Lerp(fromLightParam.multiplier, toLightParam.multiplier, f)
        };

        if (sun != null && skyVolume != null && fogVolume != null)
        {
            sun.color = currentLightParam.sunColor;
            sun.intensity = currentLightParam.sunIntensity;
            skyVolume.sunSize.value = currentLightParam.sunSize;
            skyVolume.sunSizeConvergence.value = currentLightParam.sunSizeConvergence;
            skyVolume.atmosphereThickness.value = currentLightParam.atmoThickness;
            skyVolume.exposure.value = currentLightParam.exposure;
            skyVolume.multiplier.value = currentLightParam.multiplier;

            skyVolume.skyTint.value = currentLightParam.skyColor;
            skyVolume.groundColor.value = currentLightParam.groundColor;
            fogVolume.color.value = Color.Lerp(currentLightParam.skyColor, currentLightParam.sunColor, 0.5f);
        }
    }

    private void SetTimeOfDayState(TimeOfDayStateTypes state)
    {
        if (currentTimeOfDayState != state)
        {
            currentTimeOfDayState = state;
            TimeOfDayChanged.Invoke(state);
        }
    }

    private void UpdateRain()
    {
        if (rain != prevRain)
        {
            foreach (var pfx in rainPfxs)
            {
                var emit = pfx.emission;
                emit.rateOverTime = rain * 100f;
            }
        }
        prevRain = rain;
    }

    private void UpdateFog()
    {
        if (fog != prevFog)
            fogVolume.fogDistance.value = Mathf.Lerp(1000f, 10, fog);
        prevFog = fog;
    }
}
