using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLightComponent : MonoBehaviour
{
    public GameObject lightMeshGO;
    public Light streetLight;

    private Material lightMat;

    private void Start()
    {
        if (lightMeshGO == null)
            lightMat = GetComponent<Renderer>().sharedMaterial;

        if (streetLight == null)
            streetLight = transform.GetComponent<Light>();
        if (streetLight == null)
            streetLight = transform.GetComponentInChildren<Light>();
        
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
        if (lightMeshGO != null)
            lightMeshGO.SetActive(state);

        if (lightMat != null)
        {
            if (state)
                lightMat.EnableKeyword("_EMISSION");
            else
                lightMat.DisableKeyword("_EMISSION");
        }

        if (streetLight != null)
            streetLight.enabled = state;
    }
}
