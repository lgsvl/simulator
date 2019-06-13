/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;

public class TimeOfDayLight : MonoBehaviour
{
    private Light streetLight;
    
    public void Init(TimeOfDayStateTypes state)
    {
        streetLight = GetComponent<Light>();
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged += OnTimeOfDayChange;
        OnTimeOfDayChange(state);
    }
    
    private void OnTimeOfDayChange(TimeOfDayStateTypes state)
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

    private void ToggleLight(bool state)
    {
        if (streetLight != null)
            streetLight.enabled = state;
    }
}
