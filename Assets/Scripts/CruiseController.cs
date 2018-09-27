/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CruiseController : MonoBehaviour
{
    public float sensitivity = 1.0f;
    VehicleController controller;

    //accel input range from -1 to 1
    public float GetAccel(float currentSpeed, float targetSpeed, float deltaTime)
    {
        return Mathf.Clamp((targetSpeed - currentSpeed) * deltaTime * sensitivity * 20f, -1f, 1f);
    }

    void Awake()
    {
        controller = GetComponent<VehicleController>();
    }

    void Update()
    {
        if (controller.driveMode == DriveMode.Cruise)
        {
            controller.accellInput = GetAccel(controller.CurrentSpeed, controller.cruiseTargetSpeed, Time.deltaTime);
        }
    }
}