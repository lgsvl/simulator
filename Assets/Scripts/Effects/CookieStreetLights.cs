/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class CookieStreetLights : DayNightEventListener
{
    public void Awake()
    {
        if(!mainLight)
            mainLight = gameObject.GetComponent<Light>();
    }

    public Light mainLight;

    protected override void OnDay()
    {
        mainLight.enabled = false;
    }

    protected override void OnSunSet()
    {
        mainLight.enabled = true;
    }

    protected override void OnSunRise()
    {

    }

    protected override void OnNight()
    {

    }
}

