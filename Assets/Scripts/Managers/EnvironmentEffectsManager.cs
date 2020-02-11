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
using Simulator;
using Simulator.Map;
using Simulator.Network;
using Simulator.Network.Core.Shared.Messaging;
using Simulator.Network.Core.Shared.Messaging.Data;
using Simulator.Network.Master;
using Simulator.Network.Shared;

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
    public struct TimeOfDayProfileOverrides
    {
        public Color SunColor;
        public ProceduralSky proceduralSky;
        public Tonemapping tonemapping;
        public Exposure exposure;
        public WhiteBalance whiteBalance;
        public ColorAdjustments colorAdjustments;
        public IndirectLightingController IndirectLightingController;
    }

    public enum TimeOfDayCycleTypes
    {
        Freeze,
        Normal,
        Double,
        Quadruple
    };

    [Space(5, order = 0)]
    [Header("PostProcessing", order = 1)]
    public Volume PostProcessingVolumePrefab;
    public Volume PostPrecessingVolume { get; private set; }
    public VolumeProfile ActiveProfile { get; private set; }
    public VolumeProfile DayProfile;
    public VolumeProfile SetRiseProfile;
    public VolumeProfile NightProfile;
    private TimeOfDayProfileOverrides activeOverrides;
    public TimeOfDayProfileOverrides dayOverrides;
    public TimeOfDayProfileOverrides nightOverrides;
    public TimeOfDayProfileOverrides setRiseOverrides;
    public Color RainSkyColor;
    private TimeOfDayProfileOverrides fromOverrides;
    private TimeOfDayProfileOverrides toOverrides;

    [Space(5, order = 0)]
    [Header("TimeOfDay", order = 1)]
    public float currentTimeOfDay = 12f;
    public double jday;
    public TimeOfDayCycleTypes currentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    public TimeOfDayStateTypes currentTimeOfDayState { get; private set; } = TimeOfDayStateTypes.Day;
    public event Action<TimeOfDayStateTypes> TimeOfDayChanged;
    public GameObject sunGO;
    private Light sun;
    private float cycleDurationSeconds = 360f;
    private float sunRiseBegin = 6.0f;
    private float sunRiseEnd = 7.0f;
    private float sunSetBegin = 17.0f;
    private float sunSetEnd = 18.0f;
    private float fromTimeOfDay;
    private float toTimeOfDay;
    private List<TimeOfDayLight> timeOfDayLights = new List<TimeOfDayLight>();

    private MapOrigin mapOrigin;
    private GpsLocation gpsLocation;

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
    private VolumetricFog volumetricFog;

    [Space(5, order = 0)]
    [Header("Cloud", order = 1)]
    [Range(0f, 1f)]
    public float cloud = 0f;
    private float prevCloud = 0f;
    public GameObject CloudPrefab;
    private Renderer cloudRenderer;
    private GameObject clouds;

    [Space(5, order = 0)]
    [Header("Wet", order = 1)]
    [Range(0f, 1f)]
    public float wet = 0f;
    private float prevWet = 0f;
    private List<GameObject> wetObjects = new List<GameObject>();
    private HashSet<Material> wetMaterials = new HashSet<Material>();
    private System.Random RandomGenerator;
    private int Seed = new System.Random().Next();

    Commands.EnvironmentState state = new Commands.EnvironmentState();

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
    }

    private void Awake()
    {
        mapOrigin = MapOrigin.Find();
        gpsLocation = mapOrigin.GetGpsLocation(Vector3.zero);
    }

    void Start()
    {
        InitEnvironmentEffects();
    }

    private void Update()
    {
        TimeOfDayCycle();
        UpdateRain();
        UpdateWet();
        UpdateFog();
        UpdateClouds();
        UpdateSunPosition();
        //UpdateMoonPosition();

        if (SimulatorManager.Instance.Network.IsMaster)
        {
            var masterManager = SimulatorManager.Instance.Network.Master;
            if (state.Fog != fog || state.Rain != rain || state.Wet != wet || state.Cloud != cloud || state.TimeOfDay != currentTimeOfDay)
            {
                state.Fog = fog;
                state.Rain = rain;
                state.Wet = wet;
                state.Cloud = cloud;
                state.TimeOfDay = currentTimeOfDay;

                var message = new BytesStack(masterManager.PacketsProcessor.Write(state), false);
                masterManager.BroadcastMessage(new Message(masterManager.Key, message, MessageType.ReliableUnordered));
            }
        }
    }

    private void InitEnvironmentEffects()
    {
        sunGO = Instantiate(sunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f));
        sun = sunGO.GetComponent<Light>();

        var dt = DateTime.Now;
        ResetTime(new DateTime(dt.Year, dt.Month, dt.Day, 12, 0, 0));
        Reset();
        PostPrecessingVolume = Instantiate(PostProcessingVolumePrefab);
        ActiveProfile = PostPrecessingVolume.profile;

        ActiveProfile.TryGet(out activeOverrides.proceduralSky);
        ActiveProfile.TryGet(out activeOverrides.tonemapping);
        ActiveProfile.TryGet(out activeOverrides.exposure);
        ActiveProfile.TryGet(out activeOverrides.whiteBalance);
        ActiveProfile.TryGet(out activeOverrides.colorAdjustments);
        ActiveProfile.TryGet(out activeOverrides.IndirectLightingController);

        DayProfile.TryGet(out dayOverrides.proceduralSky);
        DayProfile.TryGet(out dayOverrides.tonemapping);
        DayProfile.TryGet(out dayOverrides.exposure);
        DayProfile.TryGet(out dayOverrides.whiteBalance);
        DayProfile.TryGet(out dayOverrides.colorAdjustments);
        DayProfile.TryGet(out dayOverrides.IndirectLightingController);

        NightProfile.TryGet(out nightOverrides.proceduralSky);
        NightProfile.TryGet(out nightOverrides.tonemapping);
        NightProfile.TryGet(out nightOverrides.exposure);
        NightProfile.TryGet(out nightOverrides.whiteBalance);
        NightProfile.TryGet(out nightOverrides.colorAdjustments);
        NightProfile.TryGet(out nightOverrides.IndirectLightingController);

        SetRiseProfile.TryGet(out setRiseOverrides.proceduralSky);
        SetRiseProfile.TryGet(out setRiseOverrides.tonemapping);
        SetRiseProfile.TryGet(out setRiseOverrides.exposure);
        SetRiseProfile.TryGet(out setRiseOverrides.whiteBalance);
        SetRiseProfile.TryGet(out setRiseOverrides.colorAdjustments);
        SetRiseProfile.TryGet(out setRiseOverrides.IndirectLightingController);

        ActiveProfile.TryGet(out volumetricFog);

        clouds = Instantiate(CloudPrefab, new Vector3(0f, 100f, 0f), Quaternion.identity);
        cloudRenderer = clouds.GetComponentInChildren<Renderer>();

        rainVolumes.AddRange(FindObjectsOfType<RainVolume>());
        foreach (var volume in rainVolumes)
            rainPfxs.Add(volume.Init(rainPfx, RandomGenerator.Next()));

        wetObjects.AddRange(GameObject.FindGameObjectsWithTag("Road"));
        wetObjects.AddRange(GameObject.FindGameObjectsWithTag("Sidewalk"));
        var renderers = new List<Renderer>();
        var materials = new List<Material>();
        foreach (var obj in wetObjects)
        {
            obj.GetComponentsInChildren(renderers);
            renderers.ForEach(r =>
            {
                if (r.GetComponent<ParticleSystem>() != null || r.GetComponent<ReflectionProbe>() != null)
                {
                    return;
                }

                r.GetSharedMaterials(materials);
                materials.ForEach(m =>
                {
                    if (m == null)
                    {
                        Debug.LogError($"Object {r.gameObject.name} has null material", r.gameObject);
                    }
                    else
                    {
                        wetMaterials.Add(m);
                    }
                });
            });
        }
        SetWet();

        timeOfDayLights.AddRange(FindObjectsOfType<TimeOfDayLight>());
        timeOfDayLights.ForEach(x => x.Init(currentTimeOfDayState));
        Array.ForEach(FindObjectsOfType<TimeOfDayBuilding>(), x => x.Init(currentTimeOfDayState));
        TimeOfDayCycle();
    }

    public void Reset()
    {
        var config = Loader.Instance?.SimConfig;
        if (config != null)
        {
            fog = config.Fog;
            rain = config.Rain;
            wet = config.Wetness;
            cloud = config.Cloudiness;
            var dateTime = config.TimeOfDay;
            ResetTime(dateTime);
        }

        state.Fog = fog;
        state.Rain = rain;
        state.Wet = wet;
        state.Cloud = cloud;
        state.TimeOfDay = currentTimeOfDay;

        RandomGenerator = new System.Random(Seed);
    }

    void ResetTime(DateTime dateTime)
    {
        var tz = mapOrigin.TimeZone;

        var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Unspecified), tz);
        jday = SunMoonPosition.GetJulianDayFromGregorianDateTime(utcMidnight);

        SunMoonPosition.GetSunRiseSet(tz, dateTime, gpsLocation.Longitude, gpsLocation.Latitude, out sunRiseBegin, out sunRiseEnd, out sunSetBegin, out sunSetEnd);

        currentTimeOfDay = (float)dateTime.TimeOfDay.TotalHours;
        currentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    }

    private void UpdateSunPosition()
    {
        sun.transform.rotation = SunMoonPosition.GetSunPosition(jday + currentTimeOfDay / 24f, gpsLocation.Longitude, gpsLocation.Latitude);
    }

    private void TimeOfDayCycle()
    {
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
        if (currentTimeOfDay >= 24)
            currentTimeOfDay = 0f;

        float morning = (sunRiseBegin + sunRiseEnd) / 2.0f;
        float evening = (sunSetBegin + sunSetEnd) / 2.0f;

        if (currentTimeOfDay < sunRiseBegin)
        {
            fromOverrides = nightOverrides;
            toOverrides = nightOverrides;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }
        else if (currentTimeOfDay < morning)
        {
            fromOverrides = nightOverrides;
            toOverrides = setRiseOverrides;
            fromTimeOfDay = sunRiseBegin;
            toTimeOfDay = morning;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (currentTimeOfDay < sunRiseEnd)
        {
            fromOverrides = setRiseOverrides;
            toOverrides = dayOverrides;
            fromTimeOfDay = morning;
            toTimeOfDay = sunRiseEnd;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (currentTimeOfDay < sunSetBegin)
        {
            fromOverrides = dayOverrides;
            toOverrides = dayOverrides;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Day);
        }
        else if (currentTimeOfDay < evening)
        {
            fromOverrides = dayOverrides;
            toOverrides = setRiseOverrides;
            fromTimeOfDay = sunSetBegin;
            toTimeOfDay = evening;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else if (currentTimeOfDay < sunSetEnd)
        {
            fromOverrides = setRiseOverrides;
            toOverrides = nightOverrides;
            fromTimeOfDay = evening;
            toTimeOfDay = sunSetEnd;
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else
        {
            fromOverrides = nightOverrides;
            toOverrides = nightOverrides;
            fromTimeOfDay = 0f;
            toTimeOfDay = 0f;
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }

        //if (rain != 0f)
        //    toOverrides = rainOverrides;

        TimeOfDayColorChange();
    }

    private void TimeOfDayColorChange()
    {
        float f = Mathf.InverseLerp(fromTimeOfDay, toTimeOfDay, currentTimeOfDay);
        activeOverrides.proceduralSky.atmosphereThickness.value = Mathf.Lerp(fromOverrides.proceduralSky.atmosphereThickness.value, toOverrides.proceduralSky.atmosphereThickness.value, f);
        activeOverrides.tonemapping.mode.value = toOverrides.tonemapping.mode.value;
        activeOverrides.exposure.compensation.value = Mathf.Lerp(fromOverrides.exposure.compensation.value, toOverrides.exposure.compensation.value, f);
        activeOverrides.whiteBalance.temperature.value = Mathf.Lerp(fromOverrides.whiteBalance.temperature.value, toOverrides.whiteBalance.temperature.value, f);
        activeOverrides.colorAdjustments.contrast.value = Mathf.Lerp(fromOverrides.colorAdjustments.contrast.value, toOverrides.colorAdjustments.contrast.value, f);
        activeOverrides.colorAdjustments.colorFilter.value = Color.Lerp(fromOverrides.colorAdjustments.colorFilter.value, toOverrides.colorAdjustments.colorFilter.value, f);
        activeOverrides.colorAdjustments.saturation.value = Mathf.Lerp(fromOverrides.colorAdjustments.saturation.value, toOverrides.colorAdjustments.saturation.value, f);
        activeOverrides.IndirectLightingController.indirectDiffuseIntensity.value = Mathf.Lerp(fromOverrides.IndirectLightingController.indirectDiffuseIntensity.value, toOverrides.IndirectLightingController.indirectDiffuseIntensity.value, f);
        activeOverrides.proceduralSky.enableSunDisk.value = rain == 0f ? true : false;

        sun.color = Color.Lerp(fromOverrides.SunColor, toOverrides.SunColor, f);
        //activeOverrides.proceduralSky.skyTint.value = Color.Lerp(fromOverrides.proceduralSky.skyTint.value, RainSkyColor, rain);
    }

    private void SetTimeOfDayState(TimeOfDayStateTypes state)
    {
        if (currentTimeOfDayState != state)
        {
            currentTimeOfDayState = state;
            TimeOfDayChanged?.Invoke(state);
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

        foreach (var m in wetMaterials)
        {
            m.SetFloat("_RainIntensity", rain);
        }

        prevRain = rain;
    }

    private void UpdateFog()
    {
        if (fog != prevFog)
            volumetricFog.meanFreePath.value = Mathf.Lerp(200f, 10f, fog);
        prevFog = fog;
    }

    private void UpdateWet()
    {
        if (wet != prevWet)
            SetWet();
        prevWet = wet;
    }

    private void SetWet()
    {
        var puddle = Mathf.Clamp01((wet - 1 / 3f) * 3 / 2f);
        var damp = Mathf.Clamp01(wet * 3 / 2f);

        foreach (var m in wetMaterials)
        {
            if (wet != 0f)
            {
                m.SetFloat("_RainEffects", 1f);
                m.SetFloat("_Dampness", damp);
                m.SetFloat("_WaterLevel", puddle);
            }
            else
            {
                m.SetFloat("_RainEffects", 0f);
                m.SetFloat("_Dampness", 0f);
                m.SetFloat("_WaterLevel", 0f);
            }
        }
    }

    private void UpdateClouds()
    {
        cloudRenderer.material.SetColor("_SunCloudsColor", sun.color);

        if (cloud != prevCloud)
        {
            cloudRenderer.material.SetFloat("_Density", Mathf.Lerp(0f, 2f, cloud));
            cloudRenderer.material.SetFloat("_Size", Mathf.Lerp(2f, 0.25f, cloud));
            cloudRenderer.material.SetFloat("_Cover", Mathf.Lerp(0f, 0.9f, cloud));
        }
        prevCloud = cloud;
    }
}
