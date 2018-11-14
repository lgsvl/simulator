using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLightComponent : DayNightEventListener
{
    public GameObject lightMeshGO;
    public Light streetLight;

    private void Start()
    {
        ToggleLight(false);
    }

    protected override void OnSunRise()
    {
        ToggleLight(false);
    }
    protected override void OnDay()
    {
        ToggleLight(false);
    }
    protected override void OnSunSet()
    {
        ToggleLight(true);
    }
    protected override void OnNight()
    {
        ToggleLight(true);
    }

    private void ToggleLight(bool state)
    {
        lightMeshGO?.SetActive(state);

        if (streetLight != null)
            streetLight.enabled = state;
    }
}
