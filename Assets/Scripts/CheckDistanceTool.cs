/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class CheckDistanceTool : MonoBehaviour {
    public Transform pointA;
    public Transform pointB;

    public float GetDistance() {
        return Vector3.Distance(pointA.position, pointB.position);
    }
}
