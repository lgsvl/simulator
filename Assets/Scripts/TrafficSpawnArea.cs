/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class TrafficSpawnArea : FilterShape
{
    void Start()
    {
        var trafSpawner = FindObjectOfType<TrafSpawner>();
        if (trafSpawner != null)
        {
            if (!trafSpawner.trafficSpawnAreas.Contains(this))
            {
                trafSpawner.trafficSpawnAreas.Add(this);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}
