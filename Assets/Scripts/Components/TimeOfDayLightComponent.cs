/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class TimeOfDayLightComponent : MonoBehaviour
{
    private Light streetLight;

    private void Awake()
    {
        streetLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        Missive.AddListener<TimeOfDayMissive>(OnDayNightChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<TimeOfDayMissive>(OnDayNightChange);
    }

    public void InitLight(TimeOfDayStateTypes state)
    {
        switch (state)
        {
            case TimeOfDayStateTypes.Day:
                ToggleLight(false);
                break;
            case TimeOfDayStateTypes.Night:
                ToggleLight(true);
                break;
            case TimeOfDayStateTypes.Sunrise:
                ToggleLight(false);
                break;
            case TimeOfDayStateTypes.Sunset:
                ToggleLight(true);
                break;
        }
    }

    private void OnDayNightChange(TimeOfDayMissive missive)
    {
        switch (missive.state)
        {
            case TimeOfDayStateTypes.Day:
                ToggleLight(false);
                break;
            case TimeOfDayStateTypes.Night:
                ToggleLight(true);
                break;
            case TimeOfDayStateTypes.Sunrise:
                ToggleLight(false);
                break;
            case TimeOfDayStateTypes.Sunset:
                ToggleLight(true);
                break;
        }
    }

    private void ToggleLight(bool state)
    {
        if (streetLight != null)
            streetLight.enabled = state;
    }
}
