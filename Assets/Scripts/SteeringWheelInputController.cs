/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SteeringWheelInputController : MonoBehaviour, IInputController
{
    private PedalInputController pedalInput;

    public event Action<InputEvent> TriggerDown;
    public event Action<InputEvent> TriggerPress;
    public event Action<InputEvent> TriggerRelease;

    public float SteerInput { get; private set; }
    public float AccelBrakeInput { get; private set; }

    const float SENSITIVITY = 100f;

    private static SteeringWheelInputController inited = null;

    private int constant = 0;
    private int damper = 0;
    private int springSaturation = 0;
    private int springCoefficient = 0;

    private bool forceFeedbackPlaying = false;

    private int wheelIndex = 0;

    public float FFBGain = 1f;

    [System.NonSerialized]
    public bool available;
    private uint oldPov = 0xffffffff;
    private int[] oldButtons = new int[32];

    private readonly Dictionary<int, InputEvent> buttonMapping = new Dictionary<int, InputEvent>
    {
        // G920 button mapping -- D-Pad is treated as a Hat Switch, so it's logically an axis.
        // 0 = A
        // 1 = B
        // 2 = X
        // 3 = Y
        // 4 = Right Shift Paddle
        // 5 = Left Shift Paddle
        // 6 = "Menu" the 3-bars
        // 7 = The two boxes between LSB and the D-Pad
        // 8 = RSB
        // 9 = LSB
        // 10 = X-Box button

        { 8, InputEvent.GEARBOX_SHIFT_UP },
        { 9, InputEvent.GEARBOX_SHIFT_DOWN },
        //{ 6, InputEvent.ENABLE_HANDBRAKE },
        //{ 7, InputEvent.HEADLIGHT_MODE_CHANGE },
        //{ 8, InputEvent.ENABLE_RIGHT_TURN_SIGNAL },
        //{ 9, InputEvent.ENABLE_LEFT_TURN_SIGNAL },
        { 10, InputEvent.TOGGLE_IGNITION },
    };

    public void Init()
    {
        if (pedalInput == null)
        {
            pedalInput = GetComponent<PedalInputController>();
        }

        InitWheel();
    }

    void Start()
    {
        Init();
    }

    void InitWheel()
    {
        forceFeedbackPlaying = true;

        if (inited == null)
        {
            inited = this;
        }
        else
        {
            return;
        }

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            try
            {
                DirectInputWrapper.Init();
            }
            catch (DllNotFoundException)
            {
                // in case DirectInput wrapper dll file is not found
                available = false;
                return;
            }

            for (int i = 0; i < DirectInputWrapper.DevicesCount(); i++)
            {
                if (!DirectInputWrapper.HasForceFeedback(i)) continue;
                wheelIndex = i;
                available = true;
                break;
            }

            if (!available)
            {
                Debug.Log("STEERINGWHEEL: Multiple devices and couldn't find steering wheel device index");
                return;
            }
        }
        else
        {
            // WARNING: Input.GetJoystickNames or GetAxis/Buttons will crash if no valid Joystick is connected
            available = Environment.GetEnvironmentVariable("SDL_GAMECONTROLLERCONFIG") != null;
            // if (available)
            // {
            //     foreach (var joy in Input.GetJoystickNames())
            //     {
            //         Debug.Log($"Available joystick: {joy}, Preconfigured = {Input.IsJoystickPreconfigured(joy)}");
            //     }
            // }
        }
    }

    IEnumerator SpringforceFix()
    {
        yield return new WaitForSeconds(1f);
        StopSpringForce();
        yield return new WaitForSeconds(0.5f);
        InitSpringForce(0, 0);
    }

    void OnDestroy()
    {
        //destroy if single one, otherwise transfer control
        if (inited == this)
        {
            inited = null;
            var other = FindObjectOfType<SteeringWheelInputController>();
            if (other != null)
            {
                other.Init();
            }
            else
            {
                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
                {
                    DirectInputWrapper.Close();
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            DirectInputWrapper.Close();
        }
    }

    public void CleanUp()
    {
        forceFeedbackPlaying = false;
        constant = 0;
        damper = 0;
    }

    public void SetConstantForce(int force)
    {
        constant = force;
    }

    public void SetDamperForce(int force)
    {
        damper = force;
    }

    public void InitSpringForce(int sat, int coeff)
    {
        if (available && SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            StartCoroutine(_InitSpringForce(sat, coeff));
        }
    }

    public void StopSpringForce()
    {
        if (available && SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            DirectInputWrapper.StopSpringForce(wheelIndex);
        }
    }

    private IEnumerator _InitSpringForce(int sat, int coeff)
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitForSeconds(1f);
        long res = -1;
        int tries = 0;
        while (res < 0)
        {
            res  = DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(sat * FFBGain), Mathf.RoundToInt(coeff * FFBGain));
            tries++;
            if(tries > 150)
            {
                break;
            }

            yield return null;
        }
    }

    public void SetSpringForce(int sat, int coeff)
    {
        springCoefficient = coeff;
        springSaturation = sat;
    }

    private IEnumerator InitForceFeedback()
    {
        constant = 0;
        damper = 0;
        springCoefficient = 0;
        springSaturation = 0;
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(0.5f);
        forceFeedbackPlaying = true;
    }

    private float timeAccumulator = 0.0f;
    private bool accelPressedOnce = false;
    private bool brakePressedOnce = false;

    public void OnUpdate()
    {
        if (inited != this || !available)
        {
            return;
        }

        float accelInput;
        uint pov;
        byte[] buttons;

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            if (DirectInputWrapper.DevicesCount() == 0)
            {
                timeAccumulator += UnityEngine.Time.deltaTime;
                if (timeAccumulator >= 1.0f)
                {
                    InitWheel();
                    timeAccumulator = 0.0f;
                }
            }

            DirectInputWrapper.Update();

            DeviceState state;
            if (!DirectInputWrapper.GetStateManaged(wheelIndex, out state))
            {
                return;
            }
            SteerInput = state.lX / 32768f;
            accelInput = (state.lY - state.lRz) / -32768f;            

            if (forceFeedbackPlaying)
            {
                DirectInputWrapper.PlayConstantForce(wheelIndex, Mathf.RoundToInt(constant * FFBGain));
                DirectInputWrapper.PlayDamperForce(wheelIndex, Mathf.RoundToInt(damper * FFBGain));
                DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(springSaturation * FFBGain), springCoefficient);
            }
            
            pov = state.rgdwPOV[0];
            buttons = state.rgbButtons;
        }
        else
        {
            SteerInput = Input.GetAxis("Steering");

            // pedal range is -1 (not pressed) to +1 (pressed)
            // but by default when user has not pressed pedal the Unity reports value 0
            float accel = Input.GetAxis("Acceleration");
            float brake = Input.GetAxis("Braking");
            if (accel != 0.0f)
            {
                accelPressedOnce = true;
            }
            if (brake != 0.0f)
            {
                brakePressedOnce = true;
            }
            if (!accelPressedOnce)
            {
                accel = -1.0f;
            }
            if (!brakePressedOnce)
            {
                brake = -1.0f;
            }
            accelInput = (accel - brake) / 2.0f;

            pov = 0;
            // TODO
            // if (Input.GetButton("SelectUp")) pov = 0;
            // else if (Input.GetButton("SelectRight")) pov = 9000;
            // else if (Input.GetButton("SelectDown")) pov = 18000;
            // else if (Input.GetButton("SelectLeft")) pov = 27000;

            buttons = new byte [32];
            // TODO: here fill buttons according to buttonMapping array above
            buttons[8] = (byte)(Input.GetButton("ShiftUp") ? 1 : 0);
            buttons[9] = (byte)(Input.GetButton("ShiftDown") ? 1 : 0);
            buttons[10] = (byte)(Input.GetButton("EngineStartStop") ? 1 : 0);
        }

        if (accelInput >= 0)
        {
            AccelBrakeInput = pedalInput == null ? accelInput : pedalInput.throttleInputCurve.Evaluate(accelInput);
        }
        else
        {
            AccelBrakeInput = pedalInput == null ? accelInput: -pedalInput.brakeInputCurve.Evaluate(-accelInput);
        }

        foreach (var m in buttonMapping)
        {
            if (buttons[m.Key] != 0)
            {
                TriggerDown(m.Value);
            }

            if (oldButtons[m.Key] == 0 && buttons[m.Key] != 0)
            {
                TriggerPress(m.Value);
            }
            else if (oldButtons[m.Key] != 0 && buttons[m.Key] == 0)
            {
                TriggerRelease(m.Value);
            }
        }

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
        {
            System.Array.Copy(buttons, oldButtons, oldButtons.Length);
        }

        Action<uint, Action<InputEvent>> povAction = (uint value, Action<InputEvent> action) =>
        {
            switch (value)
            {
                case 0:
                    action(InputEvent.SELECT_UP);
                    break;
                case 9000:
                    action(InputEvent.SELECT_RIGHT);
                    break;
                case 18000:
                    action(InputEvent.SELECT_DOWN);
                    break;
                case 27000:
                    action(InputEvent.SELECT_LEFT);
                    break;
                default:
                    break;
            }
        };

        povAction(pov, x => TriggerDown(x));

        if (pov != oldPov)
        {
            povAction(oldPov, x => TriggerRelease(x));
            povAction(pov, x => TriggerPress(x));
        }

        oldPov = pov;
    }

    void MoveCar(InputEvent type)
    {
        switch (type)
        {
            case InputEvent.SELECT_UP:
                AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 70f, SENSITIVITY * Time.deltaTime);
                break;
            case InputEvent.SELECT_DOWN:
                AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, -70f, SENSITIVITY * Time.deltaTime);
                break;
            default:
                AccelBrakeInput = Mathf.MoveTowards(AccelBrakeInput, 0f, SENSITIVITY * Time.deltaTime);
                break;
        }
    }
}