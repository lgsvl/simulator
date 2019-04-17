using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeOfDayLightComponent : MonoBehaviour
{
    public Light streetLight;

    private void Awake()
    {
        streetLight = GetComponent<Light>();
    }

    private void Start()
    {
        ToggleLight(false);
        // TODO check time of day
    }

    private void OnEnable()
    {
        Missive.AddListener<TimeOfDayMissive>(OnDayNightChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<TimeOfDayMissive>(OnDayNightChange);
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
            default:
                break;
        }
    }

    private void ToggleLight(bool state)
    {
        if (streetLight != null)
            streetLight.enabled = state;
    }
}
