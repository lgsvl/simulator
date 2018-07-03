/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

[RequireComponent(typeof(WheelCollider))]
public class WheelColliderFixScript : MonoBehaviour
{
    public float wheelDampingRate;
    void Start()
    {
        GetComponent<WheelCollider>().wheelDampingRate = wheelDampingRate;
    }
}
