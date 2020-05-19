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
    private List<Material> allBuildingMaterials = new List<Material>();
    private Color emitColor = new Color(6f, 6f, 6f);

    public void Init(TimeOfDayStateTypes state)
    {
        var materials = new List<Material>();
        var sharedMaterials = new List<Material>();
        var mapping = new Dictionary<Material, Material>();

        Array.ForEach(transform.GetComponentsInChildren<Renderer>(), renderer =>
        {
            if (Application.isEditor)
            {
                renderer.GetSharedMaterials(sharedMaterials);
                renderer.GetMaterials(materials);

                Debug.Assert(sharedMaterials.Count == materials.Count);

                for (int i = 0; i < materials.Count; i++)
                {
                    if (sharedMaterials[i] == null)
                    {
                        Debug.LogError($"{renderer.gameObject.name} has null material", renderer.gameObject);
                    }
                    else
                    {
                        if (mapping.TryGetValue(sharedMaterials[i], out var mat))
                        {
                            DestroyImmediate(materials[i]);
                            materials[i] = mat;
                        }
                        else
                        {
                            mapping.Add(sharedMaterials[i], materials[i]);
                        }
                    }
                }

                renderer.materials = materials.ToArray();
            }
            else
            {
                renderer.GetSharedMaterials(materials);
                materials.ForEach(m =>
                {
                    if (!mapping.ContainsKey(m))
                    {
                        mapping.Add(m, m);
                    }
                });
            }
        });
        allBuildingMaterials.AddRange(mapping.Values);
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
        allBuildingMaterials.ForEach(material => material?.SetVector("_EmissiveColor", color));
    }
}
