/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public abstract class DayNightEventListener : MonoBehaviour {

    protected virtual void OnEnable()
    {
        if (DayNightEvents.IsInstantiated)
        {
            DayNightEvents.Instance.OnSunRise += OnSunRise;
            DayNightEvents.Instance.OnDay += OnDay;
            DayNightEvents.Instance.OnSunSet += OnSunSet;
            DayNightEvents.Instance.OnNight += OnNight;
        }
    }

    protected virtual void OnDisable()
    {
        if(DayNightEvents.IsInstantiated)
        {
            DayNightEvents.Instance.OnSunRise -= OnSunRise;
            DayNightEvents.Instance.OnDay -= OnDay;
            DayNightEvents.Instance.OnSunSet -= OnSunSet;
            DayNightEvents.Instance.OnNight -= OnNight;
        }
    }

    protected abstract void OnSunRise();
    protected abstract void OnDay();
    protected abstract void OnSunSet();
    protected abstract void OnNight();
}
