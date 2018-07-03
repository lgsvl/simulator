/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.Collections.Generic;
using UnityEngine; 

[RequireComponent(typeof(Renderer))]
public class RoadLevelMaterialRegister : MonoBehaviour
{
    public List<Material> normalMats;
    public List<Material> damageLevel1Mats;
    public List<Material> damageLevel2Mats;

    public void PickMaterial(int level)
    {
        List<Material> materials;
        switch (level)
        {
            case 0:
                materials = normalMats;
                break;
            case 1:
                materials = damageLevel1Mats;
                break;
            case 2:
                materials = damageLevel2Mats;
                break;
            default:
                materials = normalMats;
                break;
        }

        if (materials.Count > 0)
        {
            GetComponent<Renderer>().material = materials[Random.Range(0, materials.Count)];
        }
    }
}
