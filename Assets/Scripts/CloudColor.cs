/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using UnityEngine;

public class CloudColor : MonoBehaviour
{ 
    private Light sun;
    public Material cloudMaterial;

    private void Start()
    {
        sun = SimulatorManager.Instance.EnvironmentEffectsManager.sunGO.GetComponent<Light>();
    }

    void Update()
    {
        cloudMaterial.SetColor("_SunCloudsColor", sun.color);
    }
}
