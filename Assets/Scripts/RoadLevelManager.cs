/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class RoadLevelManager : MonoBehaviour
{
    public float normalPercentage;
    public float damageLevel1Percentage;
    public float damageLevel2Percentage;

    public void ConfigureRoadMaterials()
    {
        var roadLevelRegisters = GetComponentsInChildren<RoadLevelMaterialRegister>();

        var totalPercentage = normalPercentage + damageLevel1Percentage + damageLevel2Percentage;

        if (totalPercentage == 0)
        {
            Debug.Log("Total percentage can not be zero");
            return;
        }

        foreach (var register in roadLevelRegisters)
        {
            if (Random.value < normalPercentage / totalPercentage)
            {
                register.PickMaterial(0);
            }
            else if (Random.value < damageLevel1Percentage / (totalPercentage - normalPercentage))
            {
                register.PickMaterial(1);
            }
            else
            {
                register.PickMaterial(2);
            }
        }
    }
}
