/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TugbotController : AgentController, Comm.BridgeClient
{
    static readonly string CMD_VEL_TOPIC = "/wheels_controller/cmd_vel";
    static readonly string CENTER_GRIPPER_SRV = "/central_controller/center_gripper";
    static readonly string ATTACH_GRIPPER_SRV = "/central_controller/attach_gripper";

    public enum ControlMethod
    {
        UnityKeyboard,
        ROS,
    };
    private ControlMethod controlMethod;
    private float vertical;
    private float horizontal;
    private float UnityVerticalSensitivity = 100f;
    private float UnityHorizontalSensitivity = 100f;
    //private float ROSVerticalSensitivity = 3f;
    //private float ROSHorizontalSensitivity = 1.5f;
    private float wheelLeftVel;
    private float wheelRightVel;
    private TugbotHookComponent hook;

    [System.Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor;
        public bool steering;
    }

    public List<AxleInfo> axleInfos;
    public float maxMotorTorque;
    public float maxSteeringDiff;
    public float ROSWheelForceScaler = 350.0f; //use when being controled by ROS wheel command

    private Vector3 initialMainTransformPos;
    private Quaternion initialMainTransformRot;
    private float lastTimeReceived;
    private float frequency = 5; // rate command sent. [Hz]
    Comm.Bridge Bridge;

    void Start()
    {
        hook = GetComponent<TugbotHookComponent>();
        initialMainTransformPos = transform.position;
        initialMainTransformRot = transform.rotation;
    }

    public void GetSensors(List<Component> sensors)
    {
        // not used ???
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            Bridge.AddReader<Ros.Twist>(CMD_VEL_TOPIC, (Ros.Twist msg) =>
            {
                float WHEEL_SEPARATION = 0.515f;
                //float WHEEL_DIAMETER = 0.39273163f;
                float SCALING_RATIO = 0.208f;
                // Assuming that we only get linear in x and angular in z
                double v = msg.linear.x;
                double w = msg.angular.z;

                wheelLeftVel = SCALING_RATIO * (float)(v - w * 0.5 * WHEEL_SEPARATION);
                wheelRightVel = SCALING_RATIO * (float)(v + w * 0.5 * WHEEL_SEPARATION);

                lastTimeReceived = Time.time;
            });
            
            Bridge.AddService<Ros.Srv.Empty, Ros.Srv.Empty>(CENTER_GRIPPER_SRV, msg =>
            {
                hook.CenterHook();
                return new Ros.Srv.Empty();
            });

            Bridge.AddService<Ros.Srv.SetBool, Ros.Srv.SetBoolResponse>(ATTACH_GRIPPER_SRV, msg =>
            {
                hook.EngageHook(msg.data);
                return new Ros.Srv.SetBoolResponse() { success = true, message = "" };
            });
        };
    }
    
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            if (ROSAgentManager.Instance.GetIsCurrentActiveAgent(gameObject))
            {
                controlMethod = ControlMethod.UnityKeyboard;

                if (Input.GetKey(KeyCode.UpArrow))
                {
                    vertical += Time.deltaTime * UnityVerticalSensitivity;
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    vertical -= Time.deltaTime * UnityVerticalSensitivity;
                }
                else
                {
                    if (vertical > 0f)
                    {
                        vertical -= Time.deltaTime * UnityVerticalSensitivity;
                        if (vertical < 0f)
                        {
                            vertical = 0f;
                        }
                    }
                    else if (vertical < 0f)
                    {
                        vertical += Time.deltaTime * UnityVerticalSensitivity;
                        if (vertical > 0f)
                        {
                            vertical = 0f;
                        }
                    }
                }

                if (Input.GetKey(KeyCode.RightArrow))
                {
                    horizontal += Time.deltaTime * UnityHorizontalSensitivity;
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    horizontal -= Time.deltaTime * UnityHorizontalSensitivity;
                }
                else
                {
                    if (horizontal > 0f)
                    {
                        horizontal -= Time.deltaTime * UnityHorizontalSensitivity;
                        if (horizontal < 0f)
                        {
                            horizontal = 0f;
                        }
                    }
                    else if (horizontal < 0f)
                    {
                        horizontal += Time.deltaTime * UnityHorizontalSensitivity;
                        if (vertical > 0f)
                        {
                            horizontal = 0f;
                        }
                    }
                }
            }
        }
        else
        {
            controlMethod = ControlMethod.ROS;
        }
        
        if (Bridge != null && Bridge.Status != Comm.BridgeStatus.Connected)
        {
            wheelLeftVel = wheelRightVel = 0.0f;
        }

        if (Time.time - lastTimeReceived > 1/frequency)
        {
            wheelLeftVel = wheelRightVel = 0.0f;
        }

        vertical = Mathf.Clamp(vertical, -1f, 1f);
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);


        SetWheelMeshPose();
    }

    public void FixedUpdate()
    {
        if (controlMethod == ControlMethod.ROS)
        {
            float leftMotor = wheelLeftVel * ROSWheelForceScaler;
            float rightMotor = wheelRightVel * ROSWheelForceScaler;

            foreach (AxleInfo axleInfo in axleInfos)
            {
                if (leftMotor == 0)
                {
                    if (axleInfo.leftWheel.motorTorque != 0)
                    {
                        axleInfo.leftWheel.motorTorque = 0f;
                    }
                }
                else
                {
                    axleInfo.leftWheel.motorTorque = leftMotor;
                }

                if (rightMotor == 0)
                {
                    if (axleInfo.rightWheel.motorTorque != 0)
                    {
                        axleInfo.rightWheel.motorTorque = 0f;
                    }
                }
                else
                {
                    axleInfo.rightWheel.motorTorque = rightMotor;
                }
            }
        }
        else
        {
            float motor = maxMotorTorque * vertical;

            float steeringDif = maxSteeringDiff * horizontal;

            float leftMotor = 0f;
            float rightMotor = 0f;

            foreach (AxleInfo axleInfo in axleInfos)
            {
                if (axleInfo.motor)
                {
                    if (motor == 0f)
                    {
                        leftMotor = 0f;
                        rightMotor = 0f;
                    }
                    else
                    {
                        leftMotor = motor;
                        rightMotor = motor;
                    }
                }

                if (axleInfo.steering)
                {
                    if (steeringDif != 0f)
                    {
                        leftMotor += steeringDif;
                        rightMotor -= steeringDif;
                    }
                }

                if (leftMotor == 0)
                {
                    if (axleInfo.leftWheel.motorTorque != 0)
                    {
                        axleInfo.leftWheel.motorTorque = 0f;
                    }
                }
                else
                {
                    axleInfo.leftWheel.motorTorque = leftMotor;
                }

                if (rightMotor == 0)
                {
                    if (axleInfo.rightWheel.motorTorque != 0)
                    {
                        axleInfo.rightWheel.motorTorque = 0f;
                    }
                }
                else
                {
                    axleInfo.rightWheel.motorTorque = rightMotor;
                }
            }
        }
    }

    private void SetWheelMeshPose()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
    }

    private void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0); // Can be improved

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    public override void SetWheelScale(float value)
    {
        ROSWheelForceScaler = value;
    }

    public override void ResetPosition()
    {
        Vector3 posOffset = initialMainTransformPos - transform.position;
        Quaternion rotOffset = initialMainTransformRot * Quaternion.Inverse(transform.rotation);

        var tugbotHookC = GetComponent<TugbotHookComponent>();
        if (tugbotHookC != null)
        {
            if (tugbotHookC.IsHooked)
                tugbotHookC.ToggleHooked();
        }

        var allRigidBodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in allRigidBodies)
        {
            //rb.isKinematic = true;
            rb.position += posOffset;
            rb.rotation = rotOffset * rb.rotation;
            //rb.isKinematic = false;
        }
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        // TODO not functioning in duckie maps
    }
}
