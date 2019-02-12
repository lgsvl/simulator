using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLightComponent : MonoBehaviour
{
    public GameObject lightMeshGO;
    public Light streetLight;

    private void Start()
    {
        ToggleLight(false);
    }

    private void OnEnable()
    {
        Missive.AddListener<DayNightMissive>(OnDayNightChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<DayNightMissive>(OnDayNightChange);
    }

    private void OnDayNightChange(DayNightMissive missive)
    {
        switch (missive.state)
        {
            case DayNightStateTypes.Day:
                ToggleLight(false);
                break;
            case DayNightStateTypes.Night:
                ToggleLight(true);
                break;
            case DayNightStateTypes.Sunrise:
                ToggleLight(false);
                break;
            case DayNightStateTypes.Sunset:
                ToggleLight(true);
                break;
            default:
                break;
        }
    }
    
    private void ToggleLight(bool state)
    {
        lightMeshGO?.SetActive(state);

        if (streetLight != null)
            streetLight.enabled = state;
    }
}
