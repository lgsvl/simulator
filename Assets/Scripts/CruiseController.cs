/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CruiseController : MonoBehaviour
{
    public float sensitivity = 1.0f;
    VehicleController controller;
    private Toggle cruiseControlCheckbox = null;
    private Slider cruiseControlSpeedSlider = null;

    //accel input range from -1 to 1
    public float GetAccel(float currentSpeed, float targetSpeed, float deltaTime)
    {
        return Mathf.Clamp((targetSpeed - currentSpeed) * deltaTime * sensitivity * 20f, -1f, 1f);
    }

    void Awake()
    {
        controller = GetComponent<VehicleController>();
        AddUIElement();
    }

    void Update()
    {
        if (controller != null && controller.driveMode == DriveMode.Cruise)
        {
            controller.accellInput = GetAccel(controller.CurrentSpeed, controller.cruiseTargetSpeed, Time.deltaTime);
        }
    }

    private void AddUIElement()
    {
        if (controller == null)
        {
            return;
        }
        
        var targetEnv = transform.GetComponent<AgentSetup>().TargetRosEnv;
        if (targetEnv == ROSTargetEnvironment.LGSVL || targetEnv == ROSTargetEnvironment.AUTOWARE || targetEnv == ROSTargetEnvironment.APOLLO)
        {
            cruiseControlCheckbox = GetComponent<UserInterfaceTweakables>().AddCheckbox("CruiseControl", "Cruise Control:", false);
            cruiseControlCheckbox.onValueChanged.AddListener(x =>
            {
                controller.ToggleCruiseMode(controller.cruiseTargetSpeed);
            });

            float initCruiseSpeed = 10f;
            cruiseControlSpeedSlider = GetComponent<UserInterfaceTweakables>().AddFloatSlider("CruiseControlSpeed", "Cruise Control Speed (m/s):", 0f, 50f, initCruiseSpeed);
            cruiseControlSpeedSlider.onValueChanged.AddListener(speed =>
            {
                controller.cruiseTargetSpeed = speed;
            });

            controller.cruiseTargetSpeed = initCruiseSpeed;
        }
    }

    public Toggle GetToggle()
    {
        return cruiseControlCheckbox;
    }

    public Slider GetSlider()
    {
        return cruiseControlSpeedSlider;
    }
}