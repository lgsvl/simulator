/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System;
using System.Collections.Generic;

public class KeyboardInputController : MonoBehaviour, IInputController
{
    public event Action<InputEvent> TriggerDown;
    public event Action<InputEvent> TriggerPress;
    public event Action<InputEvent> TriggerRelease;

    public float SteerInput { get; private set; }
    public float AccelBrakeInput { get; private set; }

    const float SENSITIVITY = 3f;

    Dictionary<KeyCode, InputEvent> events = new Dictionary<KeyCode, InputEvent>
    {
        { KeyCode.LeftBracket, InputEvent.ENABLE_LEFT_TURN_SIGNAL },
        { KeyCode.RightBracket, InputEvent.ENABLE_RIGHT_TURN_SIGNAL },
        { KeyCode.PageUp, InputEvent.GEARBOX_SHIFT_UP },
        { KeyCode.PageDown, InputEvent.GEARBOX_SHIFT_DOWN },
        { KeyCode.RightShift, InputEvent.ENABLE_HANDBRAKE },
        { KeyCode.LeftShift, InputEvent.HEADLIGHT_MODE_CHANGE },
        { KeyCode.Keypad5, InputEvent.SET_WIPER_OFF},
        { KeyCode.Keypad6, InputEvent.SET_WIPER_AUTO},
        { KeyCode.Keypad7, InputEvent.SET_WIPER_LOW},
        { KeyCode.Keypad8, InputEvent.SET_WIPER_MID},
        { KeyCode.Keypad9, InputEvent.SET_WIPER_HIGH},
        { KeyCode.V, InputEvent.CHANGE_CAM_VIEW},
        { KeyCode.End, InputEvent.TOGGLE_IGNITION},
        { KeyCode.L, InputEvent.TOGGLE_CRUISE_MODE},
    };

    public void OnUpdate()
    {
        foreach (var e in events)
        {
            if (Input.GetKey(e.Key))
            {
                TriggerDown(e.Value);
            }
            if (Input.GetKeyDown(e.Key))
            {
                TriggerPress(e.Value);
            }
            if (Input.GetKeyUp(e.Key))
            {
                TriggerRelease(e.Value);
            }
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 1f, SENSITIVITY * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, -1f, SENSITIVITY * Time.deltaTime);
        }
        else
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 0f, SENSITIVITY * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            SteerInput = Mathf.MoveTowards(SteerInput, -1f, SENSITIVITY * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            SteerInput = Mathf.MoveTowards(SteerInput, 1f, SENSITIVITY * Time.deltaTime);
        }
        else
        {
            SteerInput = Mathf.MoveTowards(SteerInput, 0f, SENSITIVITY * Time.deltaTime);
        }
    }

    public void Init()
    {
    }

    public void CleanUp()
    {
    }
}
