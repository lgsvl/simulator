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
using UnityEngine.Rendering.HighDefinition;
using Simulator;
using Simulator.Map;
using Simulator.Network.Core.Messaging;
using Simulator.Network.Shared;
using UnityEngine.VFX;

public enum TimeOfDayStateTypes
{
    Day,
    Night,
    Sunrise,
    Sunset
};

public class EnvironmentEffectsManager : MonoBehaviour
{
    SimulationConfig Config;

    [System.Serializable]
    public struct TimeOfDayProfileOverrides
    {
        public Color SunColor;
        //public ProceduralSky proceduralSky;
        //public Tonemapping tonemapping;
        //public Exposure exposure;
        //public WhiteBalance whiteBalance;
        //public ColorAdjustments colorAdjustments;
        //public IndirectLightingController IndirectLightingController;
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
    //public VolumeProfile DayProfile;
    //public VolumeProfile SetRiseProfile;
    //public VolumeProfile NightProfile;
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
    // TODO mem issue
    //[Space(5, order = 0)]
    //[Header("Rain", order = 1)]
    //public GameObject RainEffectPrefab;
    //private float RainAmountMax = 100000f;
    //private GameObject RainEffect;
    //private List<VisualEffect> ActiveRainVfxs = new List<VisualEffect>();

    [Space(5, order = 0)]
    [Header("Fog", order = 1)]
    [Range(0f, 1f)]
    public float fog = 0f;
    private float prevFog = 0f;
    private Fog volumetricFog;
    private float MaxFog = 5000f;

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
        UpdateSunPosition();
        UpdateClouds();
        //UpdateMoonPosition();
        UpdateConfig();

        if (Loader.Instance.Network.IsMaster)
        {
            var masterManager = Loader.Instance.Network.Master;
            if (state.Fog != fog || state.Rain != rain || state.Wet != wet || state.Cloud != cloud || state.TimeOfDay != currentTimeOfDay)
            {
                state.Fog = fog;
                state.Rain = rain;
                state.Wet = wet;
                state.Cloud = cloud;
                state.TimeOfDay = currentTimeOfDay;

                var stateData = masterManager.PacketsProcessor.Write(state);
                var message = MessagesPool.Instance.GetMessage(stateData.Length);
                message.AddressKey = masterManager.Key;
                message.Content.PushBytes(stateData);
                message.Type = DistributedMessageType.ReliableOrdered;
                masterManager.BroadcastMessage(message);
            }
        }
    }

    private void InitEnvironmentEffects()
    {
        Config = Loader.Instance?.SimConfig;

        sunGO = Instantiate(sunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f));
        sun = sunGO.GetComponent<Light>();

        var dt = DateTime.Now;
        ResetTime(new DateTime(dt.Year, dt.Month, dt.Day, 12, 0, 0));
        Reset();
        PostPrecessingVolume = Instantiate(PostProcessingVolumePrefab);
        ActiveProfile = PostPrecessingVolume.profile;

        ActiveProfile.TryGet(out volumetricFog);

        clouds = Instantiate(CloudPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
        cloudRenderer = clouds.GetComponentInChildren<Renderer>();

        //ActiveProfile.TryGet(out activeOverrides.proceduralSky);
        //ActiveProfile.TryGet(out activeOverrides.tonemapping);
        //ActiveProfile.TryGet(out activeOverrides.exposure);
        //ActiveProfile.TryGet(out activeOverrides.whiteBalance);
        //ActiveProfile.TryGet(out activeOverrides.colorAdjustments);
        //ActiveProfile.TryGet(out activeOverrides.IndirectLightingController);

        //DayProfile.TryGet(out dayOverrides.proceduralSky);
        //DayProfile.TryGet(out dayOverrides.tonemapping);
        //DayProfile.TryGet(out dayOverrides.exposure);
        //DayProfile.TryGet(out dayOverrides.whiteBalance);
        //DayProfile.TryGet(out dayOverrides.colorAdjustments);
        //DayProfile.TryGet(out dayOverrides.IndirectLightingController);

        //NightProfile.TryGet(out nightOverrides.proceduralSky);
        //NightProfile.TryGet(out nightOverrides.tonemapping);
        //NightProfile.TryGet(out nightOverrides.exposure);
        //NightProfile.TryGet(out nightOverrides.whiteBalance);
        //NightProfile.TryGet(out nightOverrides.colorAdjustments);
        //NightProfile.TryGet(out nightOverrides.IndirectLightingController);

        //SetRiseProfile.TryGet(out setRiseOverrides.proceduralSky);
        //SetRiseProfile.TryGet(out setRiseOverrides.tonemapping);
        //SetRiseProfile.TryGet(out setRiseOverrides.exposure);
        //SetRiseProfile.TryGet(out setRiseOverrides.whiteBalance);
        //SetRiseProfile.TryGet(out setRiseOverrides.colorAdjustments);
        //SetRiseProfile.TryGet(out setRiseOverrides.IndirectLightingController);

        rainVolumes.AddRange(FindObjectsOfType<RainVolume>());
        foreach (var volume in rainVolumes)
            rainPfxs.Add(volume.Init(rainPfx, RandomGenerator.Next()));

        // TODO mem issue
        //RainEffect = Instantiate(RainEffectPrefab, Vector3.zero, Quaternion.identity);
        //ActiveRainVfxs.AddRange(RainEffect.GetComponentsInChildren<VisualEffect>());

        //RaycastHit hit;
        //var seed = Convert.ToUInt32(RandomGenerator.Next());
        //for (int i = 0; i < ActiveRainVfxs.Count; i++)
        //{
        //    ActiveRainVfxs[i].SetFloat("_RainfallAmount", 0f);
        //    ActiveRainVfxs[i].startSeed = seed;
        //    var fxBounds = ActiveRainVfxs[i].GetVector3("_RainfallFXBounds");
        //    var wp = ActiveRainVfxs[i].transform.position;
        //    Vector3[] boundsToCheck =
        //    {
        //        new Vector3(wp.x + fxBounds.x/2f, fxBounds.y, wp.z + fxBounds.z/2f),
        //        new Vector3(wp.x + fxBounds.x/2f, fxBounds.y, wp.z - fxBounds.z/2f),
        //        new Vector3(wp.x - fxBounds.x/2f, fxBounds.y, wp.z - fxBounds.z/2f),
        //        new Vector3(wp.x - fxBounds.x/2f, fxBounds.y, wp.z + fxBounds.z/2f)
        //    };

        //    bool isHit = false;
        //    foreach (var pt in boundsToCheck)
        //    {
        //        if (Physics.Raycast(pt, ActiveRainVfxs[i].transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
        //        {
        //            isHit = true;
        //            continue;
        //        }
        //    }

        //    if (!isHit)
        //    {
        //        ActiveRainVfxs[i].Stop();
        //        ActiveRainVfxs[i].enabled = false;
        //    }
        //}

        wetObjects.AddRange(GameObject.FindGameObjectsWithTag("Road"));
        wetObjects.AddRange(GameObject.FindGameObjectsWithTag("Sidewalk"));
        var renderers = new List<Renderer>();
        var materials = new List<Material>();
        foreach (var obj in wetObjects)
        {
            obj.GetComponentsInChildren(renderers);
            renderers.ForEach(r =>
            {
                r.GetSharedMaterials(materials);
                materials.ForEach(m =>
                {
                    if (r.GetComponent<ParticleSystem>() != null || r.GetComponent<ReflectionProbe>() != null)
                    {
                        return;
                    }

                    if (m == null)
                    {
                        Debug.Log($"Object {r.gameObject.name} has null material", r.gameObject);
                        return;
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
        if (Config != null)
        {
            if (SimulatorManager.Instance.IsAPI)
            {
                Config.Fog = 0f;
                Config.Rain = 0f;
                Config.Wetness = 0f;
                Config.Cloudiness = 0f;
                Config.TimeOfDay = new DateTime(1980, 3, 24, 12, 0, 0);
            }
            fog = Config.Fog;
            rain = Config.Rain;
            wet = Config.Wetness;
            cloud = Config.Cloudiness;
            var dateTime = Config.TimeOfDay;
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
        //float f = Mathf.InverseLerp(fromTimeOfDay, toTimeOfDay, currentTimeOfDay);
        //activeOverrides.proceduralSky.atmosphereThickness.value = Mathf.Lerp(fromOverrides.proceduralSky.atmosphereThickness.value, toOverrides.proceduralSky.atmosphereThickness.value, f);
        //activeOverrides.tonemapping.mode.value = toOverrides.tonemapping.mode.value;
        //activeOverrides.exposure.compensation.value = Mathf.Lerp(fromOverrides.exposure.compensation.value, toOverrides.exposure.compensation.value, f);
        //activeOverrides.whiteBalance.temperature.value = Mathf.Lerp(fromOverrides.whiteBalance.temperature.value, toOverrides.whiteBalance.temperature.value, f);
        //activeOverrides.colorAdjustments.contrast.value = Mathf.Lerp(fromOverrides.colorAdjustments.contrast.value, toOverrides.colorAdjustments.contrast.value, f);
        //activeOverrides.colorAdjustments.colorFilter.value = Color.Lerp(fromOverrides.colorAdjustments.colorFilter.value, toOverrides.colorAdjustments.colorFilter.value, f);
        //activeOverrides.colorAdjustments.saturation.value = Mathf.Lerp(fromOverrides.colorAdjustments.saturation.value, toOverrides.colorAdjustments.saturation.value, f);
        //activeOverrides.IndirectLightingController.indirectDiffuseIntensity.value = Mathf.Lerp(fromOverrides.IndirectLightingController.indirectDiffuseIntensity.value, toOverrides.IndirectLightingController.indirectDiffuseIntensity.value, f);
        //activeOverrides.proceduralSky.enableSunDisk.value = rain == 0f ? true : false;
        //activeOverrides.proceduralSky.skyTint.value = Color.Lerp(fromOverrides.proceduralSky.skyTint.value, RainSkyColor, rain);

        //sun.color = Color.Lerp(fromOverrides.SunColor, toOverrides.SunColor, f);
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

        // TODO mem issue
        //if (rain != prevRain)
        //{
        //    foreach (var vfx in ActiveRainVfxs)
        //    {
        //        vfx.SetFloat("_RainfallAmount", Mathf.Lerp(0f, RainAmountMax, rain));
        //    }
        //}

        foreach (var m in wetMaterials)
        {
            m.SetFloat("_RainIntensity", rain);
        }

        prevRain = rain;
    }

    private void UpdateFog()
    {
        if (fog != prevFog)
        {
            MaxFog = fog == 0 ? 5000f : 1000f;
            volumetricFog.meanFreePath.value = Mathf.Lerp(MaxFog, 25f, fog);
        }
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
        if (cloud != prevCloud)
        {
            cloudRenderer.material.SetFloat("_Density", Mathf.Lerp(0f, 1f, cloud));
            cloudRenderer.material.SetFloat("_Size", Mathf.Lerp(0f, 1f, cloud));
            cloudRenderer.material.SetFloat("_Cover", Mathf.Lerp(0f, 1f, cloud));
        }
        prevCloud = cloud;
    }

    private void UpdateConfig()
    {
        if (Config != null)
        {
            Config.Fog = fog;
            Config.Rain = rain;
            Config.Wetness = wet;
            Config.Cloudiness = cloud;
            Config.TimeOfDay = Config.TimeOfDay.Date + TimeSpan.FromHours(currentTimeOfDay);
        }
    }
}
