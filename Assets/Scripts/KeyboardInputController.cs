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
    public bool enable = true;

    public enum KeyboardMode
    {
        ARROWS,
        //WASD,
    }
    public KeyboardMode keyboardMode = KeyboardMode.ARROWS;

    public event Action<InputEvent> TriggerDown;
    public event Action<InputEvent> TriggerPress;
    public event Action<InputEvent> TriggerRelease;

    public float SteerInput { get; private set; }
    public float AccelBrakeInput { get; private set; }

    public float accel_sensitivity = 3f;
    public float steer_sensiticity = 0.3f;

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

    public void Enable()
    {
        enable = true;
    }

    public void Disable()
    {
        enable = false;
    }

    public void Init()
    {
    }

    public void OnUpdate()
    {
        if (!enable)
        {
            return;
        }

        var Up = KeyCode.None;
        var Down = KeyCode.None;
        var Left = KeyCode.None;
        var Right = KeyCode.None;

        if (keyboardMode == KeyboardMode.ARROWS)
        {
            Up = KeyCode.UpArrow;
            Down = KeyCode.DownArrow;
            Left = KeyCode.LeftArrow;
            Right = KeyCode.RightArrow;
        }

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

        if (Input.GetKey(Up))
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 1f, accel_sensitivity * Time.deltaTime);
        }
        else if (Input.GetKey(Down))
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, -1f, accel_sensitivity * Time.deltaTime);
        }
        else
        {
            AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 0f, accel_sensitivity * Time.deltaTime);
        }

        if (Input.GetKey(Left))
        {
            SteerInput = Mathf.MoveTowards(SteerInput, -1f, steer_sensiticity * Time.deltaTime);
        }
        else if (Input.GetKey(Right))
        {
            SteerInput = Mathf.MoveTowards(SteerInput, 1f, steer_sensiticity * Time.deltaTime);
        }
        else
        {
            SteerInput = Mathf.MoveTowards(SteerInput, 0f, 5f * steer_sensiticity * Time.deltaTime);
        }
    }

    public void CleanUp()
    {
    }
}
