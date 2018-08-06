/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class BridgeLights : DayNightEventListener 
{

    protected override void OnDay()
    {
        foreach(Transform t in transform) {
            t.gameObject.SetActive(false);
        }
    }

    protected override void OnNight()
    {
        foreach(Transform t in transform) {
            t.gameObject.SetActive(true);
        }
    }

    protected override void OnSunRise()
    {
    }

    protected override void OnSunSet()
    {
    }
}
