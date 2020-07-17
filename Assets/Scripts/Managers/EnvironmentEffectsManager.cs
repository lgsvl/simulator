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

public enum TimeOfDayStateTypes
{
    Day,
    Night,
    Sunrise,
    Sunset
};

public class EnvironmentEffectsManager : MonoBehaviour
{
    private SimulationConfig Config;

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

    [Space(5, order = 0)]
    [Header("TimeOfDay", order = 1)]
    [Range(0,24)]
    public float CurrentTimeOfDay = 12f;
    public TimeOfDayCycleTypes CurrentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    public event Action<TimeOfDayStateTypes> TimeOfDayChanged;

    [Space(5, order = 0)]
    [Header("Prefabs", order = 1)]
    public GameObject SunGO;
    public ParticleSystem RainPfx;
    public GameObject CloudPrefab;
    public GameObject TireSprayPrefab;

    // Sun
    private Light Sun;
    private double JDay;
    private float CycleDurationSeconds = 360f;
    private float SunRiseBegin = 6.0f;
    private float SunRiseEnd = 7.0f;
    private float SunSetBegin = 17.0f;
    private float SunSetEnd = 18.0f;
    public TimeOfDayStateTypes CurrentTimeOfDayState { get; private set; } = TimeOfDayStateTypes.Day;
    private List<TimeOfDayLight> TimeOfDayLights = new List<TimeOfDayLight>();

    private MapOrigin MapOrigin;
    private GpsLocation GPSLocation;

    [Space(5, order = 0)]
    [Header("Effect Values", order = 1)]
    [Range(0f, 1f)]
    public float Rain = 0f;
    private float PrevRain = 0f;
    private List<RainVolume> RainVolumes = new List<RainVolume>();
    private List<ParticleSystem> RainPfxs = new List<ParticleSystem>();

    [Range(0f, 1f)]
    public float Fog = 0f;
    private float PrevFog = 0f;
    private Fog VolumetricFog;
    private float MaxFog = 5000f;

    [Range(0f, 1f)]
    public float Cloud = 0f;
    private float PrevCloud = 0f;
    private Renderer CloudRenderer;

    [Range(0f, 1f)]
    public float Wet = 0f;
    private float PrevWet = 0f;
    private List<GameObject> WetObjects = new List<GameObject>();
    private HashSet<Material> WetMaterials = new HashSet<Material>();

    [Range(0f, 1f)]
    public float Damage = 0f;
    private float PrevDamage = 0f;
    private List<GameObject> DamageObjects = new List<GameObject>();
    private HashSet<Material> DamageMaterials = new HashSet<Material>();

    private System.Random RandomGenerator;
    private int Seed = new System.Random().Next();

    Commands.EnvironmentState State = new Commands.EnvironmentState();

    public void InitRandomGenerator(int seed)
    {
        Seed = seed;
        RandomGenerator = new System.Random(Seed);
    }

    private void Awake()
    {
        MapOrigin = MapOrigin.Find();
        GPSLocation = MapOrigin.GetGpsLocation(Vector3.zero);
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
        UpdateDamage();
        UpdateConfig();

        if (Loader.Instance.Network.IsMaster)
        {
            var masterManager = Loader.Instance.Network.Master;
            if (State.Fog != Fog || State.Rain != Rain || State.Wet != Wet || State.Cloud != Cloud || State.TimeOfDay != CurrentTimeOfDay)
            {
                State.Fog = Fog;
                State.Rain = Rain;
                State.Wet = Wet;
                State.Cloud = Cloud;
                State.TimeOfDay = CurrentTimeOfDay;

                var stateData = masterManager.PacketsProcessor.Write(State);
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

        Sun = Instantiate(SunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Light>();

        var dt = DateTime.Now;
        ResetTime(new DateTime(dt.Year, dt.Month, dt.Day, 12, 0, 0));
        Reset();
        PostPrecessingVolume = Instantiate(PostProcessingVolumePrefab);
        ActiveProfile = PostPrecessingVolume.profile;

        ActiveProfile.TryGet(out VolumetricFog);

        CloudRenderer = Instantiate(CloudPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity).GetComponentInChildren<Renderer>();

        RainVolumes.AddRange(FindObjectsOfType<RainVolume>());
        foreach (var volume in RainVolumes)
        {
            RainPfxs.Add(volume.Init(RainPfx, RandomGenerator.Next()));
        }

        WetObjects.AddRange(GameObject.FindGameObjectsWithTag("Road"));
        WetObjects.AddRange(GameObject.FindGameObjectsWithTag("Sidewalk"));
        var renderers = new List<Renderer>();
        var materials = new List<Material>();
        foreach (var obj in WetObjects)
        {
            obj.GetComponentsInChildren(renderers);
            renderers.ForEach(r =>
            {
                r.GetMaterials(materials);
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
                        WetMaterials.Add(m);
                    }
                });
            });
        }
        UpdateWet();

        DamageObjects.AddRange(GameObject.FindGameObjectsWithTag("Road"));
        renderers.Clear();
        materials.Clear();
        foreach (var obj in DamageObjects)
        {
            obj.GetComponentsInChildren(renderers);
            renderers.ForEach(r =>
            {
                r.GetMaterials(materials);
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
                        DamageMaterials.Add(m);
                    }
                });
            });
        }
        UpdateDamage();

        TimeOfDayLights.AddRange(FindObjectsOfType<TimeOfDayLight>());
        TimeOfDayLights.ForEach(x => x.Init(CurrentTimeOfDayState));
        Array.ForEach(FindObjectsOfType<TimeOfDayBuilding>(), x => x.Init(CurrentTimeOfDayState));
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
                Config.Damage = 0f;
                Config.TimeOfDay = new DateTime(1980, 3, 24, 12, 0, 0);
            }
            Fog = Config.Fog;
            Rain = Config.Rain;
            Wet = Config.Wetness;
            Cloud = Config.Cloudiness;
            Damage = Config.Damage;
            var dateTime = Config.TimeOfDay;
            ResetTime(dateTime);
        }

        State.Fog = Fog;
        State.Rain = Rain;
        State.Wet = Wet;
        State.Cloud = Cloud;
        State.TimeOfDay = CurrentTimeOfDay;

        RandomGenerator = new System.Random(Seed);
    }

    void ResetTime(DateTime dateTime)
    {
        var tz = MapOrigin.TimeZone;

        var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Unspecified), tz);
        JDay = SunMoonPosition.GetJulianDayFromGregorianDateTime(utcMidnight);

        SunMoonPosition.GetSunRiseSet(tz, dateTime, GPSLocation.Longitude, GPSLocation.Latitude, out SunRiseBegin, out SunRiseEnd, out SunSetBegin, out SunSetEnd);

        CurrentTimeOfDay = (float)dateTime.TimeOfDay.TotalHours;
        CurrentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    }

    private void UpdateSunPosition()
    {
        Sun.transform.rotation = SunMoonPosition.GetSunPosition(JDay + CurrentTimeOfDay / 24f, GPSLocation.Longitude, GPSLocation.Latitude);
    }

    private void TimeOfDayCycle()
    {
        switch (CurrentTimeOfDayCycle)
        {
            case TimeOfDayCycleTypes.Freeze:
                break;
            case TimeOfDayCycleTypes.Normal:
                CurrentTimeOfDay += (24f / CycleDurationSeconds) * Time.deltaTime;
                break;
            case TimeOfDayCycleTypes.Double:
                CurrentTimeOfDay += (24f / CycleDurationSeconds) * Time.deltaTime * 2;
                break;
            case TimeOfDayCycleTypes.Quadruple:
                CurrentTimeOfDay += (24f / CycleDurationSeconds) * Time.deltaTime * 4;
                break;
            default:
                break;
        }
        if (CurrentTimeOfDay >= 24)
        {
            CurrentTimeOfDay = 0f;
        }

        float morning = (SunRiseBegin + SunRiseEnd) / 2.0f;
        float evening = (SunSetBegin + SunSetEnd) / 2.0f;

        if (CurrentTimeOfDay < SunRiseBegin)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }
        else if (CurrentTimeOfDay < morning)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (CurrentTimeOfDay < SunRiseEnd)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Sunrise);
        }
        else if (CurrentTimeOfDay < SunSetBegin)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Day);
        }
        else if (CurrentTimeOfDay < evening)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else if (CurrentTimeOfDay < SunSetEnd)
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Sunset);
        }
        else
        {
            SetTimeOfDayState(TimeOfDayStateTypes.Night);
        }
    }

    private void SetTimeOfDayState(TimeOfDayStateTypes state)
    {
        if (CurrentTimeOfDayState != state)
        {
            CurrentTimeOfDayState = state;
            TimeOfDayChanged?.Invoke(state);
        }
    }

    private void UpdateRain()
    {
        if (Rain != PrevRain)
        {
            foreach (var pfx in RainPfxs)
            {
                var emit = pfx.emission;
                emit.rateOverTime = Rain * 100f;
            }
        }

        foreach (var m in WetMaterials)
        {
            m.SetFloat("_RainIntensity", Rain);
        }

        PrevRain = Rain;
    }

    private void UpdateFog()
    {
        if (Fog != PrevFog)
        {
            MaxFog = Fog == 0 ? 5000f : 1000f;
            VolumetricFog.meanFreePath.value = Mathf.Lerp(MaxFog, 25f, Fog);
        }
        PrevFog = Fog;
    }

    private void UpdateWet()
    {
        if (Wet != PrevWet)
        {
            var puddle = Mathf.Clamp01((Wet - 1 / 3f) * 3 / 2f);
            var damp = Mathf.Clamp01(Wet * 3 / 2f);

            foreach (var m in WetMaterials)
            {
                if (Wet != 0f)
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
        PrevWet = Wet;
    }

    private void UpdateClouds()
    {
        if (Cloud != PrevCloud)
        {
            CloudRenderer.material.SetFloat("_Density", Mathf.Lerp(0f, 1f, Cloud));
            CloudRenderer.material.SetFloat("_Size", Mathf.Lerp(0f, 1f, Cloud));
            CloudRenderer.material.SetFloat("_Cover", Mathf.Lerp(0f, 1f, Cloud));
        }
        PrevCloud = Cloud;
    }

    private void UpdateDamage()
    {
        if (Damage != PrevDamage)
        {
            foreach (var m in DamageMaterials) // TODO grab damage surface materials
            {
                m.SetFloat("_Damage", Damage);
            }
        }
        PrevDamage = Damage;
    }

    private void UpdateConfig()
    {
        if (Config != null)
        {
            Config.Fog = Fog;
            Config.Rain = Rain;
            Config.Wetness = Wet;
            Config.Cloudiness = Cloud;
            Config.Damage = Damage;
            Config.TimeOfDay = Config.TimeOfDay.Date + TimeSpan.FromHours(CurrentTimeOfDay);
        }
    }

    private void VFXRain() // TODO memory issue with this approach do not use
    {
        //using UnityEngine.VFX;

        // var
        //[Space(5, order = 0)]
        //[Header("Rain", order = 1)]
        //public GameObject RainEffectPrefab;
        //private float RainAmountMax = 100000f;
        //private GameObject RainEffect;
        //private List<VisualEffect> ActiveRainVfxs = new List<VisualEffect>();

        // init
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

        // UpdateRain
        //if (rain != prevRain)
        //{
        //    foreach (var vfx in ActiveRainVfxs)
        //    {
        //        vfx.SetFloat("_RainfallAmount", Mathf.Lerp(0f, RainAmountMax, rain));
        //    }
        //}
    }
}
