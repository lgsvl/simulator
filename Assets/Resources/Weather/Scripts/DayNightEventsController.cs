/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DayNightEventsController : UnitySingleton<DayNightEventsController>
{
    [System.Serializable]
    public struct lightParameters
    {
        public Color skyColor;
        public Color horizonColor;
        public Color groundColor;
        public Color sunColor;
        public Texture skyboxTexture;

        public static lightParameters copy(lightParameters o)
        {
            return new lightParameters
            {
                skyColor = o.skyColor,
                horizonColor = o.horizonColor,
                groundColor = o.groundColor,
                sunColor = o.sunColor,
                skyboxTexture = o.skyboxTexture
            };
        }
    }

    public enum Phase
    {
        Night,
        Sunrise,
        Day,
        Sunset
    }

    public float cycleDurationSeconds = 6 * 60.0f;
    public float sunRiseBegin = 6.0f;
    public float sunRiseEnd =   7.0f;
    public float sunSetBegin = 17.0f;
    public float sunSetEnd = 18.0f;
    public lightParameters morningSky = new lightParameters {
        skyColor = new Color(0.4705882f, 0.60567915f, 1, 0),
        horizonColor = new Color(0.62662196f, 0.63306886f, 0.6985294f, 0),
        groundColor = new Color(0.20588237f, 0.19544111f, 0.17560555f, 1),
        sunColor = new Color(1, 0.35172412f, 0, 0)
    };

    public lightParameters daySky = new lightParameters
    {
        skyColor = new Color(0.6903114f, 0.8137513f, 0.83823526f, 1),
        horizonColor = new Color(0.41911763f, 0.38936004f, 0.3328287f, 1),
        groundColor = new Color(0.20588237f, 0.19544111f, 0.17560555f, 1),
        sunColor = new Color(0.9852941f, 0.95131844f, 0.8403979f, 1)
    };
    
    public lightParameters eveningSky = new lightParameters
    {
        skyColor = new Color( 0.2352941f,  0.3038541f,  1f,  0f),
        horizonColor = new Color( 0.38754323f,  0.38811594f,  0.4705882f,  0f),
        groundColor = new Color( 0.20588237f,  0.19544111f,  0.17560555f,  1f),
        sunColor = new Color( 1f,  0.31034476f,  0f,  0f),
    };

    public lightParameters nightSky = new lightParameters
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
    Slider timeOfDaySlider;
    Slider rainIntensitySlider;
    Slider fogIntensitySlider;
    Slider roadWetnessSlider;
    Toggle freezeToggle;


    public WeatherController weatherController;

    private Phase phase = Phase.Day;
    public Phase currentPhase
    {
        get { return phase; }
    }

    public List<AtmosphericEffect> atmosphericEffects = new List<AtmosphericEffect>();

    public float originalSunIntensity;

    private Material skyboxMat;

    void Start()
    {
        skyboxMat = new Material(RenderSettings.skybox);
        skyboxMat.SetTexture("_Tex", daySky.skyboxTexture);
        skyboxMat.SetTexture("_OverlayTex", daySky.skyboxTexture);
        skyboxMat.SetFloat("_Blend", 0f);
        RenderSettings.skybox = skyboxMat;

        //weather controller options
        rainIntensitySlider = Tweakables.Instance.AddFloatSlider("Rain intensity", 0, 1, weatherController.rainIntensity);
        rainIntensitySlider.onValueChanged.AddListener(x => weatherController.rainIntensity = x);

        fogIntensitySlider = Tweakables.Instance.AddFloatSlider("Fog intensity", 0, 1, weatherController.fogIntensity);
        fogIntensitySlider.onValueChanged.AddListener(x => weatherController.fogIntensity = x);

        roadWetnessSlider = Tweakables.Instance.AddFloatSlider("Road wetness", 0, 1, weatherController.roadWetness);
        roadWetnessSlider.onValueChanged.AddListener(x => weatherController.roadWetness = x);

        //master time options
        timeOfDaySlider = Tweakables.Instance.AddFloatSlider("Time of day", 0, 24, currentHour);
        timeOfDaySlider.onValueChanged.AddListener(x => currentHour = x);

        freezeToggle = Tweakables.Instance.AddCheckbox("Freeze time of day", freezeTimeOfDay);
        freezeToggle.onValueChanged.AddListener(x => freezeTimeOfDay = x);

        originalSunIntensity = RenderSettings.sun.intensity;


        // CES
        CarInputController cc = FindObjectOfType<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.SELECT_UP].Press += SetDaytimeWeather;
            cc[InputEvent.SELECT_RIGHT].Press += SetFogWeather;
            cc[InputEvent.SELECT_DOWN].Press += SetRainFogWeather;
            cc[InputEvent.SELECT_LEFT].Press += SetNightRainFogWeather;
        }
        

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
        rainIntensitySlider.value = weatherController.rainIntensity;
        fogIntensitySlider.value = weatherController.fogIntensity;
        roadWetnessSlider.value = weatherController.roadWetness;
        timeOfDaySlider.value = currentHour;
        freezeToggle.isOn = freezeTimeOfDay;
    }

    private void OnDisable()
    {
        CarInputController cc = FindObjectOfType<CarInputController>();
        if (cc != null)
        {
            cc[InputEvent.SELECT_UP].Press -= SetDaytimeWeather;
            cc[InputEvent.SELECT_RIGHT].Press -= SetFogWeather;
            cc[InputEvent.SELECT_DOWN].Press -= SetRainFogWeather;
            cc[InputEvent.SELECT_LEFT].Press -= SetNightRainFogWeather;
        }
    }

    void Update()
    {
        Light sun = RenderSettings.sun;
        lightParameters lparams;

        if (sun) {
            sun.transform.rotation = Quaternion.Euler((currentHour / 24.0f) * 360.0f - 90.0f, 0, 0);
        }

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
            if (phase != Phase.Night)
            {
                phase = Phase.Night;
                DayNightEvents.Instance.night();
            }
            lparams = lightLerp(nightSky, nightSky, 0f);
            //lparams = lightParameters.copy(nightSky);
        }
        else if(currentHour < morning)
        {
            if (phase != Phase.Sunrise)
            {
                phase = Phase.Sunrise;
                DayNightEvents.Instance.sunRise();
            }

            float f = Mathf.InverseLerp(sunRiseBegin, morning, currentHour);
            lparams = lightLerp(nightSky, morningSky, f);
        }
        else if(currentHour < sunRiseEnd)
        {
            if (phase != Phase.Sunrise)
            {
                phase = Phase.Sunrise;
                DayNightEvents.Instance.sunRise();
            }

            float f = Mathf.InverseLerp(morning, sunRiseEnd, currentHour);
            lparams = lightLerp(morningSky, daySky, f);
        }
        else if(currentHour < sunSetBegin)
        {
            if (phase != Phase.Day)
            {
                phase = Phase.Day;
                DayNightEvents.Instance.day();
            }

            lparams = lightLerp(daySky, daySky, 0f);
            //lparams = lightParameters.copy(daySky);
        }
        else if(currentHour < evening)
        {
            if (phase != Phase.Sunset)
            {
                phase = Phase.Sunset;
                DayNightEvents.Instance.sunSet();
            }

            float f = Mathf.InverseLerp(sunSetBegin, evening, currentHour);
            lparams = lightLerp(daySky, eveningSky, f);
        }
        else if(currentHour < sunSetEnd)
        {
            if (phase != Phase.Sunset)
            {
                phase = Phase.Sunset;
                DayNightEvents.Instance.sunSet();
            }

            float f = Mathf.InverseLerp(evening, sunSetEnd, currentHour);
            lparams = lightLerp(eveningSky, nightSky, f);
        }
        else
        {
            if (phase != Phase.Night)
            {
                phase = Phase.Night;
                DayNightEvents.Instance.night();
            }

            lparams = lightLerp(nightSky, nightSky, 0f);
            //lparams = lightParameters.copy(nightSky);
        }

        RenderSettings.sun.intensity = originalSunIntensity;
        foreach (var item in atmosphericEffects)
        {
            item.filterSkyParams(lparams, sun);
        }

        setLightValues(lparams);

        if (!freezeTimeOfDay)
        {
            timeOfDaySlider.value = currentHour;
        }

        //Debug.Log($"Phase : {phase}");
    }

    private void setLightValues(lightParameters p)
    {
        RenderSettings.sun.color = p.sunColor;
        RenderSettings.ambientSkyColor = p.skyColor;
        RenderSettings.ambientEquatorColor = p.horizonColor;
        RenderSettings.ambientGroundColor = p.groundColor;
        RenderSettings.skybox = skyboxMat;
        //float t = p.sunColor.grayscale;
        //RenderSettings.skybox.SetColor("_Tint", new Color(t, t, t));
    }

    private lightParameters lightLerp(lightParameters p1, lightParameters p2, float f)
    {
        lightParameters ret = new lightParameters
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
}
