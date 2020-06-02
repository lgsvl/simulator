/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class TimeOfDayBuilding : MonoBehaviour
{
    private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");

    private List<Material> allBuildingMaterials = new List<Material>();
    private Color emitColor = new Color(6f, 6f, 6f);

    public void Init(TimeOfDayStateTypes state)
    {
        var materials = new List<Material>();

        Array.ForEach(transform.GetComponentsInChildren<Renderer>(), renderer =>
        {
            renderer.GetSharedMaterials(materials);
            foreach (var material in materials)
            {
                if (!allBuildingMaterials.Contains(material))
                    allBuildingMaterials.Add(material);
            }
        });
        SimulatorManager.Instance.EnvironmentEffectsManager.TimeOfDayChanged += OnTimeOfDayChange;
        OnTimeOfDayChange(state);
    }
    
    private void OnTimeOfDayChange(TimeOfDayStateTypes state)
    {
        switch (state)
        {
            case TimeOfDayStateTypes.Day:
                UpdateBuildingMats(Color.black);
                break;
            case TimeOfDayStateTypes.Night:
                UpdateBuildingMats(emitColor);
                break;
            case TimeOfDayStateTypes.Sunrise:
                UpdateBuildingMats(emitColor);
                break;
            case TimeOfDayStateTypes.Sunset:
                UpdateBuildingMats(emitColor);
                break;
        }
    }

    private void UpdateBuildingMats(Color color)
    {
        foreach (var material in allBuildingMaterials)
        {
            if (material == null)
                continue;
            material.SetVector(EmissiveColor, color);
        }
    }
}
