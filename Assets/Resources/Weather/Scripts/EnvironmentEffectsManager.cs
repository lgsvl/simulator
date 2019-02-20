/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public enum DayNightStateTypes
{
    Day,
    Night,
    Sunrise,
    Sunset
};

public class DayNightMissive : Missive
{
    public DayNightStateTypes state;
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

    [System.Serializable]
    public struct LightParameters
    {
        public Color skyColor;
        public Color horizonColor;
        public Color groundColor;
        public Color sunColor;
        public Texture skyboxTexture;

        public static LightParameters copy(LightParameters o)
        {
            return new LightParameters
            {
                skyColor = o.skyColor,
                horizonColor = o.horizonColor,
                groundColor = o.groundColor,
                sunColor = o.sunColor,
                skyboxTexture = o.skyboxTexture
            };
        }
    }

    [Space(5, order = 0)]
    [Header("Skybox", order = 1)]
    public float cycleDurationSeconds = 6 * 60.0f;
    public float sunRiseBegin = 6.0f;
    public float sunRiseEnd = 7.0f;
    public float sunSetBegin = 17.0f;
    public float sunSetEnd = 18.0f;
    public LightParameters morningSky = new LightParameters
    {
        skyColor = new Color(0.4705882f, 0.60567915f, 1, 0),
        horizonColor = new Color(0.62662196f, 0.63306886f, 0.6985294f, 0),
        groundColor = new Color(0.20588237f, 0.19544111f, 0.17560555f, 1),
        sunColor = new Color(1, 0.35172412f, 0, 0)
    };

    public LightParameters daySky = new LightParameters
    {
        skyColor = new Color(0.6903114f, 0.8137513f, 0.83823526f, 1),
        horizonColor = new Color(0.41911763f, 0.38936004f, 0.3328287f, 1),
        groundColor = new Color(0.20588237f, 0.19544111f, 0.17560555f, 1),
        sunColor = new Color(0.9852941f, 0.95131844f, 0.8403979f, 1)
    };

    public LightParameters eveningSky = new LightParameters
    {
        skyColor = new Color(0.2352941f, 0.3038541f, 1f, 0f),
        horizonColor = new Color(0.38754323f, 0.38811594f, 0.4705882f, 0f),
        groundColor = new Color(0.20588237f, 0.19544111f, 0.17560555f, 1f),
        sunColor = new Color(1f, 0.31034476f, 0f, 0f),
    };

    public LightParameters nightSky = new LightParameters
    {
        skyColor = new Color(0.1f, 0.1f, 0.12f),
        horizonColor = new Color(0.2399229f, 0.22831963f, 0.30147058f, 0f),
        groundColor = new Color(0f, 0f, 0f, 0f),
        sunColor = new Color(0f, 0f, 0f, 0f),
    };

    public Color moonColor;

    [Range(0.0f, 24.0f)]
    public float currentHour = 8.7f;
    public bool freezeTimeOfDay = true;
    private Slider timeOfDaySlider;
    private Slider rainIntensitySlider;
    private Slider fogIntensitySlider;
    private Slider roadWetnessSlider;
    private Toggle freezeToggle;
    private float originalSunIntensity;
    private Material skyboxMat;
    private Light sun;
    private LightParameters lparams;
    public DayNightStateTypes currentDayNightState { get; private set; } = DayNightStateTypes.Day;

    [Space(5, order = 0)]
    [Header("Weather", order = 1)]
    [Range(0.0f, 1.0f)]
    public float rainIntensity;

    [Range(0.0f, 0.3f)]
    public float fogIntensity = 0.0f;

    [Range(0.0f, 1.0f)]
    public float roadWetness = 0.0f;

    public float ambientTemperatureC
    {
        get { return ambientTemperatureK - zeroK; }
        set { ambientTemperatureK = value + zeroK; }
    }
    public float ambientTemperatureK = zeroK + 22.0f;
    private const float zeroK = 273.15f;
    
    public AnimationCurve singleDropsRate = AnimationCurve.Linear(0.0f, 0.0f, 0.5f, 100.0f);
    public AnimationCurve heavyRainRate = AnimationCurve.Linear(0.5f, 0.0f, 1.0f, 100.0f);
    public AnimationCurve mistRate = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 20.0f);

    public Transform rainEffects;
    private Transform currentRainEffects;
    private ParticleSystem rainDrops;
    private ParticleSystem heavyRain;
    private ParticleSystem heavyRainFront;
    private ParticleSystem mist;
    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule m;
    private Camera agentCamera;

    // wet roads
    private float wetness = 0.0f;
    private float wetnessTarget = 0f;
    private float fadeTime = 4.0f;
    private float dryGlossiness = 0.2f;
    private float wetGlossiness = 0.8f;
    private List<Material> roadMats = new List<Material>();

    void Start()
    {
        InitDayNight();
        InitWeather();
        InitWetRoads();

        // CES TODO needs moved asap
        CarInputController cc = FindObjectOfType<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.SELECT_UP].Press += SetDaytimeWeather;
            cc[InputEvent.SELECT_RIGHT].Press += SetFogWeather;
            cc[InputEvent.SELECT_DOWN].Press += SetRainFogWeather;
            cc[InputEvent.SELECT_LEFT].Press += SetNightRainFogWeather;
        }
    }

    void Update()
    {
        UpdateDayNight();
        UpdateRoadWetness();
    }

    private void OnEnable()
    {
        Missive.AddListener<ActiveAgentMissive>(OnAgentChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<ActiveAgentMissive>(OnAgentChange);

        CarInputController cc = FindObjectOfType<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.SELECT_UP].Press -= SetDaytimeWeather;
            cc[InputEvent.SELECT_RIGHT].Press -= SetFogWeather;
            cc[InputEvent.SELECT_DOWN].Press -= SetRainFogWeather;
            cc[InputEvent.SELECT_LEFT].Press -= SetNightRainFogWeather;
        }
    }

    private void OnAgentChange(ActiveAgentMissive missive)
    {
        if (currentRainEffects == null) return;
        agentCamera = missive.agent.Agent.GetComponent<AgentSetup>().FollowCamera;
        currentRainEffects.SetParent(null);
        currentRainEffects.position = agentCamera.transform.position;
        currentRainEffects.rotation = agentCamera.transform.rotation;
        currentRainEffects.SetParent(agentCamera.transform);
    }

    private void InitDayNight()
    {
        if (sun == null)
        {
            if (RenderSettings.sun != null)
                sun = RenderSettings.sun;
            else
                sun = new Light();
        }

        skyboxMat = new Material(RenderSettings.skybox);
        skyboxMat.SetTexture("_Tex", daySky.skyboxTexture);
        skyboxMat.SetTexture("_OverlayTex", daySky.skyboxTexture);
        skyboxMat.SetFloat("_Blend", 0f);
        RenderSettings.skybox = skyboxMat;

        //weather controller options
        rainIntensitySlider = Tweakables.Instance.AddFloatSlider("Rain intensity", 0, 1, rainIntensity);
        rainIntensitySlider.onValueChanged.AddListener(x => rainIntensity = x);

        fogIntensitySlider = Tweakables.Instance.AddFloatSlider("Fog intensity", 0, 1, fogIntensity);
        fogIntensitySlider.onValueChanged.AddListener(x => fogIntensity = x);

        roadWetnessSlider = Tweakables.Instance.AddFloatSlider("Road wetness", 0, 1, roadWetness);
        roadWetnessSlider.onValueChanged.AddListener(x => roadWetness = x);

        //master time options
        timeOfDaySlider = Tweakables.Instance.AddFloatSlider("Time of day", 0, 24, currentHour);
        timeOfDaySlider.onValueChanged.AddListener(x => currentHour = x);

        freezeToggle = Tweakables.Instance.AddCheckbox("Freeze time of day", freezeTimeOfDay);
        freezeToggle.onValueChanged.AddListener(x => freezeTimeOfDay = x);

        originalSunIntensity = RenderSettings.sun.intensity;
    }

    private void UpdateDayNight()
    {
        sun.transform.rotation = Quaternion.Euler((currentHour / 24.0f) * 360.0f - 90.0f, 0, 0);

        if (!freezeTimeOfDay)
        {
            float gameHourPerRealSeconds = 24.0f / cycleDurationSeconds;
            currentHour += Time.deltaTime * gameHourPerRealSeconds;
        }

        while (currentHour >= 24.0f) currentHour -= 24.0f;

        float morning = (sunRiseBegin + sunRiseEnd) / 2.0f;
        float evening = (sunSetBegin + sunSetEnd) / 2.0f;

        if (currentHour < sunRiseBegin)
        {
            if (currentDayNightState != DayNightStateTypes.Night)
            {
                currentDayNightState = DayNightStateTypes.Night;
                DayNightStateChange();
            }
            lparams = lightLerp(nightSky, nightSky, 0f);
            //lparams = lightParameters.copy(nightSky);
        }
        else if (currentHour < morning)
        {
            if (currentDayNightState != DayNightStateTypes.Sunrise)
            {
                currentDayNightState = DayNightStateTypes.Sunrise;
                DayNightStateChange();
            }

            float f = Mathf.InverseLerp(sunRiseBegin, morning, currentHour);
            lparams = lightLerp(nightSky, morningSky, f);
        }
        else if (currentHour < sunRiseEnd)
        {
            if (currentDayNightState != DayNightStateTypes.Sunrise)
            {
                currentDayNightState = DayNightStateTypes.Sunrise;
                DayNightStateChange();
            }

            float f = Mathf.InverseLerp(morning, sunRiseEnd, currentHour);
            lparams = lightLerp(morningSky, daySky, f);
        }
        else if (currentHour < sunSetBegin)
        {
            if (currentDayNightState != DayNightStateTypes.Day)
            {
                currentDayNightState = DayNightStateTypes.Day;
                DayNightStateChange();
            }

            lparams = lightLerp(daySky, daySky, 0f);
            //lparams = lightParameters.copy(daySky);
        }
        else if (currentHour < evening)
        {
            if (currentDayNightState != DayNightStateTypes.Sunset)
            {
                currentDayNightState = DayNightStateTypes.Sunset;
                DayNightStateChange();
            }

            float f = Mathf.InverseLerp(sunSetBegin, evening, currentHour);
            lparams = lightLerp(daySky, eveningSky, f);
        }
        else if (currentHour < sunSetEnd)
        {
            if (currentDayNightState != DayNightStateTypes.Sunset)
            {
                currentDayNightState = DayNightStateTypes.Sunset;
                DayNightStateChange();
            }

            float f = Mathf.InverseLerp(evening, sunSetEnd, currentHour);
            lparams = lightLerp(eveningSky, nightSky, f);
        }
        else
        {
            if (currentDayNightState != DayNightStateTypes.Night)
            {
                currentDayNightState = DayNightStateTypes.Night;
                DayNightStateChange();
            }

            lparams = lightLerp(nightSky, nightSky, 0f);
            //lparams = lightParameters.copy(nightSky);
        }

        sun.intensity = originalSunIntensity;

        FilterSkyParams(lparams, sun);

        setLightValues(lparams);

        if (!freezeTimeOfDay)
        {
            timeOfDaySlider.value = currentHour;
        }

        UpdateWeather();
    }

    private void DayNightStateChange()
    {
        var missive = new DayNightMissive
        {
            state = currentDayNightState
        };
        Missive.Send(missive);
    }

    public void SetDaytimeWeather()
    {
        rainIntensitySlider.value = 0f;
        fogIntensitySlider.value = 0.001f;
        roadWetnessSlider.value = 0f;
        timeOfDaySlider.value = 9f;
    }

    public void SetRainFogWeather()
    {
        rainIntensitySlider.value = 0.7f;
        fogIntensitySlider.value = 0.3f;
        roadWetnessSlider.value = 1f;
        timeOfDaySlider.value = 12.5f;
    }

    public void SetNightRainFogWeather()
    {
        rainIntensitySlider.value = 0.3f;
        fogIntensitySlider.value = 0.7f;
        roadWetnessSlider.value = 0.3f;
        timeOfDaySlider.value = 19.8f;
    }

    public void SetFogWeather()
    {
        rainIntensitySlider.value = 0f;
        fogIntensitySlider.value = 0.8f;
        roadWetnessSlider.value = 0f;
        timeOfDaySlider.value = 6.7f;
    }

    public void RefreshControls()
    {
        rainIntensitySlider.value = rainIntensity;
        fogIntensitySlider.value = fogIntensity;
        roadWetnessSlider.value = roadWetness;
        timeOfDaySlider.value = currentHour;
        freezeToggle.isOn = freezeTimeOfDay;
    }
    
    private void FilterSkyParams(LightParameters lightParameters, Light celestialLight)
    {
        RenderSettings.fogColor = Color.Lerp(lightParameters.skyColor, lightParameters.sunColor, 0.5f);
        // celestialLight.intensity *= (1.0f - rainIntensity);
    }

    private void setLightValues(LightParameters p)
    {
        sun.color = p.sunColor;
        RenderSettings.ambientSkyColor = p.skyColor;
        RenderSettings.ambientEquatorColor = p.horizonColor;
        RenderSettings.ambientGroundColor = p.groundColor;
        if (RenderSettings.skybox != null)
            RenderSettings.skybox = skyboxMat;
        //float t = p.sunColor.grayscale;
        //RenderSettings.skybox.SetColor("_Tint", new Color(t, t, t));
    }

    private LightParameters lightLerp(LightParameters p1, LightParameters p2, float f)
    {
        LightParameters ret = new LightParameters
        {
            groundColor = Color.Lerp(p1.groundColor, p2.groundColor, f),
            skyColor = Color.Lerp(p1.skyColor, p2.skyColor, f),
            horizonColor = Color.Lerp(p1.horizonColor, p2.horizonColor, f),
            sunColor = Color.Lerp(p1.sunColor, p2.sunColor, f)
        };
        skyboxMat.SetTexture("_Tex", p1.skyboxTexture);
        skyboxMat.SetTexture("_OverlayTex", p2.skyboxTexture);
        skyboxMat.SetFloat("_Blend", f);
        return ret;
    }

    #region weather
    private void InitWeather()
    {
        fogIntensity = RenderSettings.fogDensity;
        
        agentCamera = ROSAgentManager.Instance.GetCurrentActiveAgent() != null ? ROSAgentManager.Instance.GetCurrentActiveAgent().GetComponent<AgentSetup>().FollowCamera : Camera.main;
        currentRainEffects = Instantiate(rainEffects);
        currentRainEffects.position = agentCamera.transform.position;
        currentRainEffects.rotation = agentCamera.transform.rotation;
        currentRainEffects.SetParent(agentCamera.transform);
        rainDrops = currentRainEffects.transform.Find("RainDropsPfx").GetComponent<ParticleSystem>();
        heavyRain = currentRainEffects.transform.Find("HeavyRainPfx").GetComponent<ParticleSystem>();
        heavyRainFront = currentRainEffects.transform.Find("HeavyRainFrontPfx").GetComponent<ParticleSystem>();
        mist = currentRainEffects.transform.Find("MistPfx").GetComponent<ParticleSystem>();
        main = heavyRainFront.main;

        // TODO why assign to same module over and over?
        m = rainDrops.emission;
        m.rateOverTimeMultiplier = 0.0f;
        m.rateOverDistanceMultiplier = 0.0f;

        m = heavyRain.emission;
        m.rateOverTimeMultiplier = 0.0f;
        m.rateOverDistanceMultiplier = 0.0f;

        m = heavyRainFront.emission;
        m.rateOverTimeMultiplier = 0.0f;
        m.rateOverDistanceMultiplier = 0.0f;

        m = mist.emission;
        m.rateOverTimeMultiplier = 0.0f;
        m.rateOverDistanceMultiplier = 0.0f;
    }
    
    private void UpdateWeather()
    {
        if (agentCamera == null) return;

        UpdateRainIntensity();
        float height = 40;
        float offset = 5;
        Vector3 startpos = new Vector3(0, height, agentCamera.velocity.magnitude * (height / main.startSpeedMultiplier) + offset);
        heavyRainFront.transform.localPosition = startpos;
        main.startLifetime = 40 / main.startSpeedMultiplier + 0.5f;
        mist.transform.localPosition = new Vector3(0, 0, agentCamera.velocity.magnitude * offset);

        //RenderSettings.fog = rainIntensity > 0 || fogIntensity > 0;
        RenderSettings.fogDensity = Mathf.Max(0.01f * rainIntensity, fogIntensity * 0.025f);
        //RenderSettings.fogMode = FogMode.ExponentialSquared;

        // remove x,z rotation
        if (currentRainEffects != null)
            currentRainEffects.eulerAngles = new Vector3(0f, currentRainEffects.eulerAngles.y, 0f);
    }

    private void UpdateRainIntensity()
    {
        float targetRate;
        targetRate = singleDropsRate.Evaluate(rainIntensity);
        m = rainDrops.emission;

        m.rateOverTimeMultiplier = targetRate;
        targetRate = heavyRainRate.Evaluate(rainIntensity);

        m = heavyRain.emission;
        m.rateOverTimeMultiplier = targetRate;
        m = heavyRainFront.emission;
        m.rateOverDistanceMultiplier = targetRate / 20.0f;

        m = mist.emission;
        m.rateOverTimeMultiplier = targetRate;
    }
    #endregion

    #region wet roads
    private void InitWetRoads()
    {
        wetness = 0f;
        wetnessTarget = 0f;
        var roadObjs = GameObject.FindGameObjectsWithTag("Road").ToList();
        foreach (GameObject item in roadObjs)
        {
            Renderer r = item.GetComponent<Renderer>();

            if (!r)
                continue;

            foreach (Material mat in r.materials)
            {
                roadMats.Add(mat);
            }
        }
    }

    private void UpdateRoadWetness()
    {
        if (roadMats == null) return;

        if (wetness != wetnessTarget)
        {
            wetness = Mathf.MoveTowards(wetness, wetnessTarget, Time.deltaTime / fadeTime);
            foreach (Material m in roadMats)
            {
                if (wetness > 0)
                {
                    m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                }
                else
                {
                    m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                }
                m.SetFloat("_Glossiness", Mathf.Lerp(dryGlossiness, wetGlossiness, wetness));
            }
        }
        wetness = Mathf.Max(roadWetness, wetness);
    }
    #endregion
}
