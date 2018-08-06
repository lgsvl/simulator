/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WetRoads : MonoBehaviour {
    [Range(0.0f, 1.0f)]
    public float wetness = 0.0f;
    [Range(0.0f, 1.0f)]
    public float wetnessTarget = 0f;
    public float fadeTime = 20f;
    [Range(0.0f, 1.0f)]
    public float dryGlossiness = 0.2f;
    [Range(0.0f, 1.0f)]
    public float wetGlossiness = 0.9f;

    private HashSet<Material> materials = new HashSet<Material>();

    public static WetRoads instance
    {
        get;
        private set;
    }

    public event System.Action<float> OnWetness;

    public void SetWetness(float wet)
    {
        wetness = wet;
        wetnessTarget = wet;

        if (OnWetness != null)
            OnWetness(wet);
    }

    public bool IsWet()
    {
        return wetnessTarget > 0.5f;
    }

    public void ToggleWet(bool val)
    {
        if (val)
            wetnessTarget = 1f;
        else
            wetnessTarget = 0f;
    }

    void Awake()
    {
        instance = this;
        wetness = 0f;
        wetnessTarget = 0f;
        SetWetness(0f);

        foreach (Transform item in transform)
        {
            Renderer r = item.GetComponent<Renderer>();
            if (!r)
            {
                print("no renderer!");
                continue;
            }
            foreach (Material mat in r.materials)
            {
                materials.Add(mat);
            }
        }
        updateMaterials();
    }

    void updateMaterials()
    {
        foreach (Material m in materials)
        {
            m.SetFloat("_Glossiness", Mathf.Lerp(dryGlossiness, wetGlossiness, wetness));
            m.SetFloat("_Bumpmapscale", 1.0f - Mathf.Lerp(dryGlossiness, wetGlossiness, wetness));
        }
    }

    void Update()
    {
        if (wetness != wetnessTarget)
        {
            wetness = Mathf.MoveTowards(wetness, wetnessTarget, Time.deltaTime / fadeTime);
            updateMaterials();
        }
        if (DayNightEventsController.Instance.weatherController)
        {
            wetnessTarget = DayNightEventsController.Instance.weatherController.rainIntensity;
            wetness = Mathf.Max(DayNightEventsController.Instance.weatherController.roadWetness, wetness);
        }

    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
