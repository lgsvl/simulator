/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class StreetLight : DayNightEventListener {

    public Light streetLight;
    public bool power = false;

    [System.NonSerialized]
    public bool inActiveRange = false;

    private float initialBounceIntensity;
    bool flickering = false;

    private void Start()
    {
        if (!streetLight)
            streetLight = transform.Find("Light").GetComponent<Light>();
        power = false;
        streetLight.enabled = false;

        initialBounceIntensity = streetLight.bounceIntensity;

        //var interval = TrafPerformanceManager.GetInstance().lightPerformanceCheckInterval;
        //InvokeRepeating("UpdateLightPerformance", Random.Range(0.0f, interval), interval);
    }

    private void UpdateLightPerformance()
    {
        var dist = Vector3.Distance(streetLight.transform.position, Camera.main.transform.position);
        if (dist < TrafPerformanceManager.GetInstance().lightIndirectDistanceThreshold)
        {
            inActiveRange = true;
            if (!flickering)
            {
                streetLight.bounceIntensity = initialBounceIntensity;
            }
        }
        else
        {
            inActiveRange = false;
            if (!flickering)
            {
                streetLight.bounceIntensity = 0.0f;
            }
        }
    }

    protected override void OnDay()
    {
        power = false;
        streetLight.enabled = false;
    }

    protected override void OnNight()
    {
        power = true;
        if (Random.Range(0.0f, 1.0f) < 0.1f)
        {
            StartCoroutine(LightFlicker());
            flickering = true;
            streetLight.bounceIntensity = 0.0f;
        }
        else
        {
            flickering = false;
            StartCoroutine(LightOnDelayed());
        }

    }

    protected override void OnSunRise()
    {
    }

    protected override void OnSunSet()
    {
    }

    IEnumerator LightOnDelayed()
    {
        float originalIntensity = streetLight.intensity;
        float dimIntensity = streetLight.intensity * 0.5f;
        yield return new WaitForSeconds(Random.Range(0.1f, 2.0f));
        streetLight.enabled = true;

        int tries = Random.Range(1, 5);
        for (int i = 0; i < tries; i++)
        {
            streetLight.intensity = originalIntensity;
            yield return new WaitForSeconds(Random.Range(0.02f*tries, 0.1f*tries));
            streetLight.intensity = dimIntensity;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.2f));
        }
        streetLight.intensity = originalIntensity;
    }

    IEnumerator LightFlicker()
    {
        float originalIntensity = streetLight.intensity;
        float dimIntensity = streetLight.intensity * 0.5f;
        streetLight.enabled = true;

        while (power)
        {
            streetLight.intensity = originalIntensity;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.1f));
            streetLight.intensity = dimIntensity;
            yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
            
        }
        streetLight.enabled = false;
    }

    private void OnDestroy()
    {
        CancelInvoke("UpdateLightPerformance");
    }
}
