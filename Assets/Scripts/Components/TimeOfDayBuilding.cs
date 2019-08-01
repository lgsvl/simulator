/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class TimeOfDayBuilding : MonoBehaviour
{
    private List<Renderer> buildingRenderers = new List<Renderer>();
    private List<Material> materials = new List<Material>(8);

    public void Init(TimeOfDayStateTypes state)
    {
        var sharedMaterials = new List<Material>(8);
        var mapping = new Dictionary<Material, Material>();
        buildingRenderers.AddRange(GetComponentsInChildren<Renderer>());
        buildingRenderers.ForEach(renderer =>
        {
            if (Application.isEditor)
            {
                renderer.GetSharedMaterials(sharedMaterials);
                renderer.GetMaterials(materials);

                Debug.Assert(sharedMaterials.Count == materials.Count);

                for (int i = 0; i < materials.Count; i++)
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

                renderer.materials = materials.ToArray();
            }
            else
            {
                renderer.GetSharedMaterials(materials);
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
                UpdateBuildingMats(Color.white, 2f);
                break;
            case TimeOfDayStateTypes.Sunrise:
                UpdateBuildingMats(Color.black);
                break;
            case TimeOfDayStateTypes.Sunset:
                UpdateBuildingMats(Color.white * 2f);
                break;
        }
    }

    private void UpdateBuildingMats(Color color, float hd = 1f)
    {
        var materials = new List<Material>(8);

        buildingRenderers.ForEach(renderer =>
        {
            if (Application.isEditor)
            {
                renderer.GetMaterials(materials);
            }
            else
            {
                renderer.GetSharedMaterials(materials);
            }
            materials.ForEach(material => material?.SetVector("_EmissiveColor", color * hd));
        });
        
    }
}
