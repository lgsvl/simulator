/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeOfDayBuilding : MonoBehaviour
{
    private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
    private List<Material> allBuildingMaterials = new List<Material>();
    private Renderer[] allRenderers;
    private Color emitColor = new Color(6f, 6f, 6f);

    public void Init(TimeOfDayStateTypes state)
    {
        allRenderers = transform.GetComponentsInChildren<Renderer>();
        var materials = new List<Material>();

        Array.ForEach(allRenderers, renderer =>
        {
            renderer.GetSharedMaterials(materials);
            foreach (var material in materials)
            {
                if (!allBuildingMaterials.Contains(material))
                    allBuildingMaterials.Add(material);
            }
        });

        if (SimulatorManager.InstanceAvailable)
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
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var r in allRenderers)
            {
                var sharedMats = r.sharedMaterials;
                var tmpMats = new Material[sharedMats.Length];

                for (var i = 0; i < sharedMats.Length; ++i)
                {
                    tmpMats[i] = new Material(sharedMats[i]);
                    tmpMats[i].SetVector(EmissiveColorId, color);
                }

                r.sharedMaterials = tmpMats;
            }
        }
        else
#endif
        {
            foreach (var material in allBuildingMaterials)
            {
                if (material == null)
                    continue;
                material.SetVector(EmissiveColorId, color);
            }
        }
    }
}