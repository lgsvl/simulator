using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherController : MonoBehaviour, AtmosphericEffect {

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

    public ParticleSystem rainDrops;
    public ParticleSystem heavyRain;
    public ParticleSystem heavyRainFront;
    public ParticleSystem mist;

    public AnimationCurve singleDropsRate = AnimationCurve.Linear(0.0f, 0.0f, 0.5f, 100.0f);
    public AnimationCurve heavyRainRate = AnimationCurve.Linear(0.5f, 0.0f, 1.0f, 100.0f);
    public AnimationCurve mistRate = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 20.0f);

    void updateRainIntensity()
    {
        float targetRate;
        targetRate = singleDropsRate.Evaluate(rainIntensity);
        ParticleSystem.EmissionModule m = rainDrops.emission;

        m.rateOverTimeMultiplier = targetRate;
        targetRate = heavyRainRate.Evaluate(rainIntensity);

        m = heavyRain.emission;
        m.rateOverTimeMultiplier = targetRate;
        m = heavyRainFront.emission;
        m.rateOverDistanceMultiplier = targetRate / 20.0f;

        m = mist.emission;
        m.rateOverTimeMultiplier = targetRate;
    }

    // Use this for initialization
    void Start ()
    {
        fogIntensity = RenderSettings.fogDensity;

        if (!rainDrops)
        {
            rainDrops = transform.Find("rain drops").GetComponent<ParticleSystem>();
        }
        if (!heavyRain)
        {
            heavyRain = transform.Find("heavy rain").GetComponent<ParticleSystem>();
        }
        if (!heavyRainFront)
        {
            heavyRainFront = transform.Find("heavy rain front emitter").GetComponent<ParticleSystem>();
        }
        if (!mist)
        {
            mist = transform.Find("mist").GetComponent<ParticleSystem>();
        }

        ParticleSystem.EmissionModule m;
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

        var tweak = Tweakables.Instance;

        var rainIntensitySlider = tweak.AddFloatSlider("Rain intensity", 0, 1, rainIntensity);
        rainIntensitySlider.onValueChanged.AddListener(x => rainIntensity = x);

        var fogIntensitySlider = tweak.AddFloatSlider("Fog intensity", 0, 1, fogIntensity);
        fogIntensitySlider.onValueChanged.AddListener(x => fogIntensity = x);

        var roadWetnessSlider = tweak.AddFloatSlider("Road wetness", 0, 1, roadWetness);
        roadWetnessSlider.onValueChanged.AddListener(x => roadWetness = x);

        DayNightEventsController.Instance.atmosphericEffects.Add(this);
    }

    private void OnDestroy()
    {
        DayNightEventsController.Instance?.atmosphericEffects.Remove(this);
    }

    // Update is called once per frame
    void Update ()
    {
        updateRainIntensity();
        //parenting causes odd issues so we reposition manually
        Transform follow = Camera.main.transform;
        Vector3 pos = follow.position;
        transform.position = pos;
        //rotate with camera but only y axis (up)
        transform.rotation = Quaternion.Euler(0, follow.rotation.eulerAngles.y, 0);

        ParticleSystem.MainModule main = heavyRainFront.main;

        float height = 40;
        float offset = 5;
        Vector3 startpos = new Vector3(0, height, Camera.main.velocity.magnitude * (height / main.startSpeedMultiplier) + offset);
        heavyRainFront.transform.localPosition = startpos;
        main.startLifetime = height / main.startSpeedMultiplier + 0.5f;

        heavyRainFront.transform.localPosition = startpos;
        main = heavyRain.main;
        main.startLifetime = height / main.startSpeedMultiplier + 0.5f;

        mist.transform.localPosition = new Vector3(0, 0, Camera.main.velocity.magnitude * 5);

        //RenderSettings.fog = rainIntensity > 0 || fogIntensity > 0;
        RenderSettings.fogDensity = Mathf.Max(0.01f * rainIntensity, fogIntensity);
        //RenderSettings.fogMode = FogMode.ExponentialSquared;
    }

    public void filterSkyParams(DayNightEventsController.lightParameters sky, Light celestialLight)
    {
        RenderSettings.fogColor = Color.Lerp(sky.skyColor, sky.sunColor, 0.5f);
        // celestialLight.intensity *= (1.0f - rainIntensity);
    }
}
