/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleMaterialComponent : MonoBehaviour
{
    [System.Serializable]
    public struct VehicleMaterialData
    {
        public Renderer renderer;
        public Material[] mats;
    }

    public VehicleMaterialData[] vehicleMaterialData;

    private void Start()
    {
        foreach (var item in vehicleMaterialData)
        {
            item.renderer.sharedMaterial = new Material(item.mats[(int) Random.Range(0, item.mats.Length)]);
        }
    }
}
