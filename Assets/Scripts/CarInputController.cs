/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections.Generic;

public enum InputEvent
{
    CHANGE_CAM_VIEW,
    HORN,
    ENABLE_LEFT_TURN_SIGNAL,
    ENABLE_RIGHT_TURN_SIGNAL,
    GEARBOX_SHIFT_DOWN,
    GEARBOX_SHIFT_UP,
    ENABLE_HANDBRAKE,
    HEADLIGHT_MODE_CHANGE,
    TOGGLE_IGNITION,
    SET_WIPER_OFF,
    SET_WIPER_AUTO,
    SET_WIPER_LOW,
    SET_WIPER_MID,
    SET_WIPER_HIGH,
    TOGGLE_CRUISE_MODE,
    SELECT_UP,
    SELECT_DOWN,
    SELECT_LEFT,
    SELECT_RIGHT,
    CUSTOM_EVENT_0,
    CUSTOM_EVENT_1,
    CUSTOM_EVENT_2,
    CUSTOM_EVENT_3,
    CUSTOM_EVENT_4,
};

public class InputAction
{
    public System.Action Down;
    public System.Action Press;
    public System.Action Release;
}

public class CarInputController : MonoBehaviour
{
    Dictionary<InputEvent, InputAction> Events = new Dictionary<InputEvent, InputAction>();

    public float SteerInput { get; private set; }
    public float AccelBrakeInput { get; private set; }

    public InputAction this[InputEvent index]
    {
        get
        {
            if (!Events.ContainsKey(index))
            {
                Events.Add(index, new InputAction());
            }
            return Events[index];
        }
    }

    List<IInputController> controllers;

    public KeyboardInputController KeyboardInput
    {
        get
        {
            foreach (var c in controllers)
            {
                if (c is KeyboardInputController)
                {
                    return (c as KeyboardInputController);
                }
            }
            return null;
        }
    }

    public SteeringWheelInputController SteerWheelInput
    {
        get
        {
            foreach (var c in controllers)
            {
                if (c is SteeringWheelInputController)
                {
                    return (c as SteeringWheelInputController);
                }
            }
            return null;
        }
    }

    void TriggerDown(InputEvent type)
    {
        if (Events.ContainsKey(type) && Events[type].Down != null)
        {
            Events[type].Down();
        }
    }

    void TriggerPress(InputEvent type)
    {
        if (Events.ContainsKey(type) && Events[type].Press != null)
        {
            Events[type].Press();
        }
    }

    void TriggerRelease(InputEvent type)
    {
        if (Events.ContainsKey(type) && Events[type].Release != null)
        {
            Events[type].Release();
        }
    }

    public void Init()
    {
        foreach (var c in controllers)
        {
            c.Init();
        }
    }

    void Start()
    {
        controllers = new List<IInputController>()
        {
            GetComponent<KeyboardInputController>(),
            GetComponent<SteeringWheelInputController>(),
        };

        controllers.RemoveAll(item => item == null);

        foreach (var c in controllers)
        {
            c.TriggerDown += TriggerDown;
            c.TriggerPress += TriggerPress;
            c.TriggerRelease += TriggerRelease;
        }        
    }

    public void Update()
    {
        SteerInput = 0.0f;
        AccelBrakeInput = 0.0f;

        foreach (var c in controllers)
        {
            c.OnUpdate();
            SteerInput += c.SteerInput;
            AccelBrakeInput += c.AccelBrakeInput;
        }
    }    

    public bool HasValidSteeringWheelInput()
    {
        var steerwheel = SteerWheelInput;
        if (steerwheel != null && steerwheel.available)
        {
            return true;
        }
        return false;
    }
}
