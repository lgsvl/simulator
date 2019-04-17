/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

public enum TimeOfDayStateTypes
{
    Day,
    Night,
    Sunrise,
    Sunset
};

public class TimeOfDayMissive : Missive
{
    public TimeOfDayStateTypes state;
}

public class EnvironmentEffectsManager : MonoBehaviour
{
    #region Singleton
    private static EnvironmentEffectsManager _instance = null;
    public static EnvironmentEffectsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<EnvironmentEffectsManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>EnvironmentEffectsManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
    }
    #endregion

    [Space(5, order = 0)]
    [Header("TimeOfDay", order = 1)]
    private float cycleDurationSeconds = 360f;
    private float sunRiseBegin = 6.0f;
    private float sunRiseEnd = 7.0f;
    private float sunSetBegin = 17.0f;
    private float sunSetEnd = 18.0f;
    private float fromTimeOfDay;
    private float toTimeOfDay;
    public TimeOfDayStateTypes currentTimeOfDayState { get; private set; } = TimeOfDayStateTypes.Day;

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
    private LightParameters fromLightParam = new LightParameters();
    private LightParameters toLightParam = new LightParameters();
    private LightParameters currentLightParam = new LightParameters();

    private LightParameters sunriseSky = new LightParameters
    {
        skyColor = new Color(0.2235f, 0.1803f, 0.4470f, 1f),
        groundColor = new Color(0.2058f, 0.1954f, 0.1756f, 1f),
        sunColor = new Color(1, 0.3517f, 0f, 1f),
        sunIntensity = 3.141593f,
        sunSize = 0.025f,
        sunSizeConvergence = 5.33f,
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
        sunSizeConvergence = 5.33f,
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
        sunSizeConvergence = 5.33f,
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
    public float currentTimeOfDay = 12f;

    public enum TimeOfDayCycleTypes
    {
        Freeze,
        Normal,
        Double,
        Quadruple
    };
    public TimeOfDayCycleTypes currentTimeOfDayCycle = TimeOfDayCycleTypes.Normal;

    public GameObject sunGO;
    private Light sun;
    private ProceduralSky skyVolume;
    private ExponentialFog fogVolume;

    List<Light> lights = new List<Light>();

    private bool isInit = false;

    private void Start()
    {
        InitEnvironmentEffects();
    }

    private void Update()
    {
        if (!isInit) return;
        TimeOfDayCycle();
    }

    private void InitEnvironmentEffects()
    {
        var sunGO = GameObject.FindGameObjectWithTag("Sun");
        sun = sunGO == null ? Instantiate(sunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Light>() : sunGO.GetComponent<Light>(); // noon TODO real pos and rotation
        skyVolume = FindObjectOfType<ProceduralSky>();
        fogVolume = FindObjectOfType<ExponentialFog>();

        
        lights.AddRange(FindObjectsOfType<Light>());
        foreach (var light in lights)
        {
            if (light.gameObject.tag != "Sun")
                light.gameObject.AddComponent<TimeOfDayLightComponent>();
        }

        isInit = true;
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
            var colorParameterSky = new ColorParameter(currentLightParam.skyColor, true);
            var colorParameterGround = new ColorParameter(currentLightParam.groundColor, true);
            var colorParameterFog = new ColorParameter(Color.Lerp(currentLightParam.skyColor, currentLightParam.sunColor, 0.5f), true);
            sun.color = currentLightParam.sunColor;
            sun.intensity = currentLightParam.sunIntensity;
            skyVolume.sunSize = new ClampedFloatParameter(currentLightParam.sunSize, 0f, 100f, true);
            skyVolume.sunSizeConvergence = new ClampedFloatParameter(currentLightParam.sunSizeConvergence, 0f, 100f, true);
            skyVolume.atmosphereThickness = new ClampedFloatParameter(currentLightParam.atmoThickness, 0f, 100f, true);
            skyVolume.exposure = new FloatParameter(currentLightParam.exposure, true);
            skyVolume.multiplier = new MinFloatParameter(currentLightParam.multiplier, 0f, true);

            skyVolume.skyTint = colorParameterSky;
            skyVolume.groundColor = colorParameterGround;
            fogVolume.color = colorParameterFog;
        }
    }

    private void SetTimeOfDayState(TimeOfDayStateTypes state)
    {
        if (currentTimeOfDayState != state)
        {
            currentTimeOfDayState = state;
            var missive = new TimeOfDayMissive
            {
                state = currentTimeOfDayState
            };
            Missive.Send(missive);
        }
    }
}
