/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BuildingLights : DayNightEventListener {

    int lightProperty;

    void Start() {
        lightProperty = Shader.PropertyToID("_LightsOn");
    }

    public void LightsOn()
    {
        Shader.SetGlobalFloat("_LightsOn", 1f);
    }

    public void LightsOff()
    {
        Shader.SetGlobalFloat("_LightsOn", 0f);
    }
        
    protected override void OnDay()
    {
        Shader.SetGlobalFloat(lightProperty, 0f);
    }

    protected override void OnNight()
    {
    }

    protected override void OnSunRise()
    {
    }

    protected override void OnSunSet()
    {
        Shader.SetGlobalFloat(lightProperty, 1f);
    }
}
