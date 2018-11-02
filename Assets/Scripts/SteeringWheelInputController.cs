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

public enum SteerWheelAutonomousFeedbackBehavior
{
    None, //Steering wheel do nothing under selfdriving mode
    OutputOnly, //Steering wheel should only read values to simulate autonomous driving, it won't consider itself as a factor of steering the car
    InputAndOutputNoRoadFeedback, //Steering wheel will not lnly read values to simulate autonomous driving but also consider itself to be the input factor to drive the car
    InputAndOutputWithRoadFeedback, //Sterring wheel will read values to simulate autonomous driving and also consider itself to be the input factor to drive the car. ground force will also give feedback on steering wheel
}

public class SteeringWheelInputController : MonoBehaviour, IInputController, IForceFeedback
{
    public bool enable = true;

    private PedalInputController pedalInput;

    public event Action<InputEvent> TriggerDown;
    public event Action<InputEvent> TriggerPress;
    public event Action<InputEvent> TriggerRelease;

    public float SteerInput { get; private set; }
    public float AccelBrakeInput { get; private set; }

    const float SENSITIVITY = 100f;

    private static SteeringWheelInputController inited = null;

    private int autoforce = 0;
    private int constant = 0;
    private int damper = 0;
    private int springSaturation = 0;
    private int springCoefficient = 0;

    public bool useFeedback = true; //master control for all types of steerwheel feedback
    [Space(5)]
    public bool useGroundFeedbackForce = false;

    public SteerWheelAutonomousFeedbackBehavior autonomousBehavior = SteerWheelAutonomousFeedbackBehavior.OutputOnly;

    private int wheelIndex = 0;
    public int WheelIndex
    {
        get
        {
            return wheelIndex;
        }
    }

    public float forceFeedbackGain = 1.5f;
    public float autoForceGain = 1.0f;

    [System.NonSerialized]
    public bool available = false;
    [System.NonSerialized]
    public string stateFail = "";
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

        { 0, InputEvent.AUTONOMOUS_MODE_OFF },
        { 1, InputEvent.TOGGLE_MAIN_CAM },
        { 8, InputEvent.GEARBOX_SHIFT_UP },
        { 9, InputEvent.GEARBOX_SHIFT_DOWN },
        //{ 6, InputEvent.ENABLE_HANDBRAKE },
        //{ 7, InputEvent.HEADLIGHT_MODE_CHANGE },
        //{ 8, InputEvent.ENABLE_RIGHT_TURN_SIGNAL },
        //{ 9, InputEvent.ENABLE_LEFT_TURN_SIGNAL },
        { 10, InputEvent.TOGGLE_IGNITION },
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
        if (pedalInput == null)
        {
            pedalInput = GetComponent<PedalInputController>();
        }

        InitWheel();
    }

    void Start()
    {
        TriggerPress += ev =>
        {
            if (ev == InputEvent.TOGGLE_MAIN_CAM)
            {
                var robot = GetComponent<RobotSetup>();
                robot.UI.MainCameraToggle.isOn = !robot.UI.MainCameraToggle.isOn;
            }
        };

        Init();
    }

    void InitWheel()
    {
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
                Debug.Log("DirectInput wrapper dll file is not found");
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
        autoforce = 0;
        constant = 0;
        damper = 0;
    }

    public void SetAutonomousForce(int force)
    {
        autoforce = force;
    }

    public void SetConstantForce(int force)
    {
        constant = force;
    }

    public void SetDamperForce(int force)
    {
        damper = force;
    }

    public void SetSpringForce(int sat, int coeff)
    {
        springSaturation = sat;
        springCoefficient = coeff;
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
            res  = DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(sat * forceFeedbackGain), Mathf.RoundToInt(coeff * forceFeedbackGain));
            tries++;
            if(tries > 150)
            {
                break;
            }

            yield return null;
        }
    }

    private IEnumerator InitForceFeedback()
    {
        autoforce = 0;
        constant = 0;
        damper = 0;
        springCoefficient = 0;
        springSaturation = 0;
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(0.5f);
    }

    private float timeAccumulator = 0.0f;
    private bool accelPressedOnce = false;
    private bool brakePressedOnce = false;

    public void OnUpdate()
    {
        if (inited != this || !available || !enable)
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
                var error = $"Can not get valid device state for steering wheel {wheelIndex}, try reconnect and restart game to fix";
                Debug.Log(error);
                stateFail = error;
                available = false;
                return;
            }
            SteerInput = state.lX / 32768f;
            accelInput = (state.lY - state.lRz) / -32768f;

            if (!useFeedback || !useGroundFeedbackForce)
            {
                DirectInputWrapper.PlayConstantForce(wheelIndex, 0);
                DirectInputWrapper.PlayDamperForce(wheelIndex, Mathf.Clamp(Mathf.RoundToInt(damper * forceFeedbackGain), -10000, 10000));
                DirectInputWrapper.PlaySpringForce(wheelIndex, 0, 0, springCoefficient);
            }
            
            if (useFeedback && (useGroundFeedbackForce || (autonomousBehavior != SteerWheelAutonomousFeedbackBehavior.None)))
            {
                float totalConstForce = 0.0f;
                if (useGroundFeedbackForce)
                {
                    totalConstForce += constant * forceFeedbackGain;                    
                }

                if (autonomousBehavior != SteerWheelAutonomousFeedbackBehavior.None)
                {
                    totalConstForce += autoforce * autoForceGain;
                }

                //applying effects to steering wheel
                DirectInputWrapper.PlayConstantForce(wheelIndex, Mathf.Clamp(Mathf.RoundToInt(totalConstForce), -10000, 10000));
                if (useGroundFeedbackForce)
                {
                    DirectInputWrapper.PlayDamperForce(wheelIndex, Mathf.Clamp(Mathf.RoundToInt(damper * forceFeedbackGain), -10000, 10000));
                    DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.Clamp(Mathf.RoundToInt(springSaturation * forceFeedbackGain), 0, 10000), springCoefficient);
                }
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
            buttons[0] = (byte)(Input.GetButton("TurnOffAutonomousMode") ? 1 : 0);
            buttons[1] = (byte)(Input.GetButton("ToggleMainCamera") ? 1 : 0);
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

        System.Array.Copy(buttons, oldButtons, oldButtons.Length);

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