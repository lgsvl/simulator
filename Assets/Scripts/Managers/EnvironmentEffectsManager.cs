/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Simulator;
using Simulator.Components;
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

    private PhysicallyBasedSky PBS;

    [Space(5, order = 0)]
    [Header("TimeOfDay", order = 1)]
    [Range(0,24)]
    public float CurrentTimeOfDay = 12f;
    public DateTime CurrentDateTime;
    public TimeOfDayCycleTypes CurrentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;
    public event Action<TimeOfDayStateTypes> TimeOfDayChanged;

    [Space(5, order = 0)]
    [Header("Prefabs", order = 1)]
    public GameObject SunGO;
    public GameObject MoonGO;
    public ParticleSystem RainPfx;
    public VFXRain VFXRainPrefab;
    public GameObject CloudPrefab;
    public GameObject TireSprayPrefab;

    // Sun moon
    public Light Sun { get; private set; }
    private HDAdditionalLightData SunHD;
    private Light Moon;
    private HDAdditionalLightData MoonHD;
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
    private VFXRain VFXRain;

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
        GPSLocation = MapOrigin.PositionToGpsLocation(Vector3.zero);
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
        UpdateMoonPosition();
        UpdateClouds();
        UpdateSunEffect();
        UpdatePhysicalSky();
        UpdateDamage();
        UpdateConfig();

        if (Loader.Instance.Network.IsMaster)
        {
            var masterManager = Loader.Instance.Network.Master;
            if (State.Fog != Fog || State.Rain != Rain || State.Wet != Wet || State.Cloud != Cloud ||
                State.Damage != Damage || State.TimeOfDay != CurrentTimeOfDay)
            {
                State.Fog = Fog;
                State.Rain = Rain;
                State.Wet = Wet;
                State.Cloud = Cloud;
                State.Damage = Damage;
                State.TimeOfDay = CurrentTimeOfDay;

                var writer = new NetDataWriter();
                masterManager.PacketsProcessor.Write(writer, State);
                var message = MessagesPool.Instance.GetMessage(writer.Length);
                message.AddressKey = masterManager.Key;
                message.Content.PushBytes(writer.CopyData());
                message.Type = DistributedMessageType.ReliableOrdered;
                masterManager.BroadcastMessage(message);
            }
        }
    }

    private void InitEnvironmentEffects()
    {
        Config = Loader.Instance?.SimConfig;

        Sun = Instantiate(SunGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Light>();
        SunHD = Sun.gameObject.GetComponent<HDAdditionalLightData>();

        Moon = Instantiate(MoonGO, new Vector3(0f, 50f, 0f), Quaternion.Euler(90f, 0f, 0f)).GetComponent<Light>();
        MoonHD = Moon.gameObject.GetComponent<HDAdditionalLightData>();

        // AD Stack needs -90 degree map rotation so celestial bodies need rotated
        var CelestialBodyHolder = new GameObject("CelestialBodyHolder");
        CelestialBodyHolder.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, -90f, 0f));
        Sun.transform.SetParent(CelestialBodyHolder.transform);
        Moon.transform.SetParent(CelestialBodyHolder.transform);

        Reset();

        PostPrecessingVolume = Instantiate(PostProcessingVolumePrefab);
        ActiveProfile = PostPrecessingVolume.profile;

        ActiveProfile.TryGet(out VolumetricFog);
        ActiveProfile.TryGet(out PBS);

        CloudRenderer = Instantiate(CloudPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity).GetComponentInChildren<Renderer>();

        VFXRain = Instantiate(VFXRainPrefab);
        var origin = MapOrigin.Find();
        var trans = VFXRain.transform;
        var pos = trans.position;
        pos.y += origin.transform.position.y;
        trans.position = pos;
        var agents = SimulatorManager.Instance.AgentManager.ActiveAgents;
        foreach (var agent in agents)
        {
            VFXRain.RegisterTrackedEntity(agent.AgentGO.transform);
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
        RandomGenerator = new System.Random(Seed);

        if (Config == null)
        {
            return;
        }

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

    private void ResetTime(DateTime dateTime)
    {
        var tz = MapOrigin.TimeZone;

        var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Unspecified), tz);
        JDay = SunMoonPosition.GetJulianDayFromGregorianDateTime(utcMidnight);

        SunMoonPosition.GetSunRiseSet(tz, dateTime, GPSLocation.Longitude, GPSLocation.Latitude, out SunRiseBegin, out SunRiseEnd, out SunSetBegin, out SunSetEnd);

        CurrentDateTime = dateTime;
        CurrentTimeOfDay = (float)dateTime.TimeOfDay.TotalHours;
        CurrentTimeOfDayCycle = TimeOfDayCycleTypes.Freeze;

        UpdateDistributedState();
    }

    public void SetDateTime(DateTime dateTime)
    {
        Config.TimeOfDay = dateTime;
        ResetTime(dateTime);
    }

    private void UpdateSunPosition()
    {
        Sun.transform.localRotation = SunMoonPosition.GetSunPosition(JDay + CurrentTimeOfDay / 24f, GPSLocation.Longitude, GPSLocation.Latitude);
    }

    private void UpdateMoonPosition()
    {
        Moon.transform.localRotation = SunMoonPosition.GetMoonPosition(JDay + CurrentTimeOfDay / 24f, GPSLocation.Longitude, GPSLocation.Latitude);
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

        int hour = (int) Mathf.Floor(CurrentTimeOfDay);
        float minf = ((CurrentTimeOfDay - hour) * 60.0f);
        int min = (int) minf;
        int sec = (int) ((minf - min) * 60.0f);
        TimeSpan ts = new TimeSpan(hour, min, sec);
        CurrentDateTime = CurrentDateTime.Date + ts;

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

        if (CurrentTimeOfDayState == TimeOfDayStateTypes.Day)
        {
            if (Moon.enabled)
            {
                Moon.enabled = false;
            }
        }
        else
        {
            if (!Moon.enabled)
            {
                Moon.enabled = true;
            }
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

    public void InitRainVFX(Transform transform)
    {
        VFXRain.RegisterTrackedEntity(transform); // API
    }

    public void ClearRainVFX(Transform transform)
    {
        VFXRain.UnregisterTrackedEntity(transform); // API
    }

    private void UpdateRain()
    {
        if (Rain != PrevRain)
        {
            VFXRain.SetIntensity(Rain);
            
            foreach (var pfx in RainPfxs)
            {
                var emit = pfx.emission;
                emit.rateOverTime = Rain * 100f;
            }

            foreach (var m in WetMaterials)
            {
                m.SetFloat("_RainIntensity", Rain);
            }

            if (Rain != 0f)
            {
                UpdateClouds(true);
                UpdateFog(true);
            }
        }
        PrevRain = Rain;
    }

    private void UpdateSunEffect()
    {
        var factor = (Rain + Cloud) / 2f;
        if (Rain != 0f || Cloud != 0f)
        {
            if (Cloud != 0f)
            {
                SunHD.flareTint = Color.Lerp(Color.white, Color.black, Cloud);
                SunHD.surfaceTint = Color.Lerp(Color.white, Color.black, Cloud);
            }
            else
            {
                SunHD.flareTint = Color.white;
                SunHD.surfaceTint = Color.white;
            }
            if (Rain >= 0.25f || Cloud >= 0.25f)
            {
                SunHD.shadowDimmer = Mathf.Lerp(1f, 0.6f, (factor - 0.25f) * 4f); // Controls shadow density when rain or clouds are on.
            }
            else
            {
                SunHD.shadowDimmer = 1f;
            }
        }
        else
        {
            SunHD.shadowDimmer = 1f;
        }
    }

    private void UpdateMoonEffect()
    {
        if (Cloud != 0)
        {
            MoonHD.flareTint = Color.Lerp(Color.grey, Color.black, Cloud);
            MoonHD.surfaceTint = Color.Lerp(Color.white, Color.black, Cloud);
        }
        else
        {
            MoonHD.flareTint = Color.grey;
            MoonHD.surfaceTint = Color.white;
        }
    }

    private void UpdatePhysicalSky()
    {
        var factor = (Rain + Cloud) / 2f;
        if (Rain != 0f || Cloud != 0f)
        {
            PBS.aerosolDensity.value = Mathf.Lerp(0.01192826f, 0.8f, factor); //darkens the sky using particle density in the atmosphere
        }
        else
        {
            PBS.aerosolDensity.value = 0.01192826f;
        }
    }

    private void UpdateFog(bool force = false)
    {
        if (Rain >= 0.15f)
        {
            VolumetricFog.maximumHeight.value = Mathf.Lerp(50f, 100f, Fog);
        }
        else
        {
            VolumetricFog.maximumHeight.value = Mathf.Lerp(25f, 50f, Fog);
        }
        MaxFog = Fog == 0f ? 5000f : 1000f;
        VolumetricFog.meanFreePath.value = Mathf.Lerp(MaxFog, 25f, Fog);
        PrevFog = Fog;
    }

    private void UpdateClouds(bool force = false)
    {
        if (Cloud != PrevCloud || force)
        {
            if (Rain != 0f)
            {
                CloudRenderer.material.SetFloat("_RainAmount", Mathf.Lerp(0f, 1f, Rain)); //Rain amount tells the shader to lerp to a darker version of the clouds
            }
            else
            {
                CloudRenderer.material.SetFloat("_RainAmount", 0);
            }
            SimulatorManager.Instance.UIManager.UpdateEnvironmentalEffectsUI();
            CloudRenderer.material.SetFloat("_Density", Mathf.Lerp(0f, 1f, Cloud));
            CloudRenderer.material.SetFloat("_Size", Mathf.Lerp(.8f, 1f, Cloud));
        }
        PrevCloud = Cloud;
    }

    private void UpdateWet()
    {
        if (Wet != PrevWet)
        {
            var puddle = Mathf.Clamp01((Wet - 1 / 3f) * 3 / 2f);
            var damp = Mathf.Clamp01(Wet);

            foreach (var m in WetMaterials)
            {
                if (Wet != 0f)
                {
                    m.SetFloat("_Dampness", damp);
                    m.SetFloat("_WaterLevel", puddle);
                }
                else
                {
                    m.SetFloat("_Dampness", 0f);
                    m.SetFloat("_WaterLevel", 0f);
                }
            }
        }
        PrevWet = Wet;
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

    private void UpdateDistributedState()
    {
        State.Fog = Fog;
        State.Rain = Rain;
        State.Wet = Wet;
        State.Cloud = Cloud;
        State.Damage = Damage;
        State.TimeOfDay = CurrentTimeOfDay;
    }
}
