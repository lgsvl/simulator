/*
 * Copyright (c) 2021 LG Electronics Inc.
 *
 * SPDX-License-Identifier: MIT
 */

using UnityEngine;

public class Motor : MonoBehaviour
{
    private RapidChangeControl _rapidControl = new RapidChangeControl();
    private MotorMotionFeedback _feedback = new MotorMotionFeedback();
    public MotorMotionFeedback Feedback => _feedback;

    private PID _pidControl = null;
    private ArticulationBody _motorBody = null;

    private bool _enableMotor = false;

    public float _targetAngularVelocity = 0;
    public float _targetTorque = 0;
    public float _currentMotorVelocity;

    public float _prevJointPosition = 0;

    public string GetMotorName()
    {
        return (_motorBody == null)? string.Empty:_motorBody.transform.parent.name;
    }

    public void SetTargetJoint(in GameObject target)
    {
        var body = target.GetComponentInChildren<ArticulationBody>();
        SetTargetJoint(body);
    }

    public void SetTargetJoint(in ArticulationBody body)
    {
        if (body.jointType.Equals(ArticulationJointType.RevoluteJoint) || body.jointType.Equals(ArticulationJointType.SphericalJoint))
        {
            _motorBody = body;
        }
        else
        {
            Debug.LogWarningFormat("joint type({0}) is not revolte!!", body.jointType);
        }
    }

    public void SetPID(in float pFactor, in float iFactor, in float dFactor)
    {
        _pidControl = new PID(pFactor, iFactor, dFactor, 50, -50, 300, -300);
    }

    public PID GetPID()
    {
        return _pidControl;
    }

    /// <summary>Get Current Joint Velocity</summary>
    /// <remarks>degree per second</remarks>
    public float GetCurrentVelocity()
    {
        return _currentMotorVelocity;
    }

    /// <summary>Set Target Velocity wmotorLeftith PID control</summary>
    /// <remarks>degree per second</remarks>
    public void SetVelocityTarget(in float targetAngularVelocity)
    {
        var compensatingVelocityRatio = 0f;

        if (Mathf.Abs(targetAngularVelocity) < float.Epsilon || targetAngularVelocity == 0)
        {
            _enableMotor = false;
        }
        else
        {
            _enableMotor = true;

            if (_feedback.IsMotionRotating)
            {
                var directionSwitch = (Mathf.Sign(_targetAngularVelocity) == Mathf.Sign(targetAngularVelocity)) ? false : true;
                _rapidControl.SetDirectionSwitched(directionSwitch);
            }

            const float compensateThreshold = 10.0f;
            const float compensatingRatio = 1.20f;

            compensatingVelocityRatio =  ((Mathf.Abs(targetAngularVelocity) < compensateThreshold) ? compensatingRatio : 1.0f);
        }

        _targetAngularVelocity = targetAngularVelocity * compensatingVelocityRatio;
    }

    void FixedUpdate()
    {
        if (_motorBody == null)
        {
            Debug.LogWarning("motor Body is empty, please set target body first");
            return;
        }
        else if (!_motorBody.jointType.Equals(ArticulationJointType.RevoluteJoint) && !_motorBody.jointType.Equals(ArticulationJointType.SphericalJoint))
        {
            Debug.LogWarning("Articulation Joint Type is wonrg => " + _motorBody.jointType);
            return;
        }

        _currentMotorVelocity = GetJointVelocity();
        // Debug.LogFormat("joint vel({0}) accel({1}) force({2}) friction({3}) pos({4})",
        // 	_motorBody.jointVelocity[0], _motorBody.jointAcceleration[0], _motorBody.jointForce[0], _motorBody.jointFriction, _motorBody.jointPosition[0]);

        // do stop motion of motor when motor disabled
        if (_enableMotor)
        {
            // Compensate target angular velocity
            var targetAngularVelocityCompensation = (_targetAngularVelocity != 0) ? (Mathf.Sign(_targetAngularVelocity) * _feedback.Compensate()) : 0;

            var compensatedTargetAngularVelocity = _targetAngularVelocity + targetAngularVelocityCompensation;

            _targetTorque = Mathf.Abs(_pidControl.Update(compensatedTargetAngularVelocity, _currentMotorVelocity, Time.fixedDeltaTime));

            // Debug.Log(GetMotorName() + ", " + _targetAngularVelocity + " <=> " + _currentMotorVelocity);

            // Improve motion for rapid direction change
            if (_rapidControl.DirectionSwitched())
            {
                Stop();
                compensatedTargetAngularVelocity = 0;
                _rapidControl.Wait();
            }

            SetTargetVelocityAndForce(compensatedTargetAngularVelocity, _targetTorque);
        }
        else
        {
            Stop();
        }
    }

    public void Stop()
    {
        _targetTorque = 0;
        SetJointVelocity(0);
        SetTargetVelocityAndForce(0, 0);
        _pidControl.Reset();
        _rapidControl.SetDirectionSwitched(false);
        _motorBody.velocity = Vector3.zero;
        _motorBody.angularVelocity = Vector3.zero;
    }

    private ArticulationDrive GetDrive()
    {
        var drive = new ArticulationDrive();

        if (_motorBody.jointType.Equals(ArticulationJointType.RevoluteJoint))
        {
            drive = _motorBody.xDrive;
        }
        else if (_motorBody.jointType.Equals(ArticulationJointType.SphericalJoint))
        {
            if (!_motorBody.twistLock.Equals(ArticulationDofLock.LockedMotion))
            {
                drive = _motorBody.xDrive;
            }
            else if (!_motorBody.swingYLock.Equals(ArticulationDofLock.LockedMotion))
            {
                drive = _motorBody.yDrive;
            }
            else if (!_motorBody.swingZLock.Equals(ArticulationDofLock.LockedMotion))
            {
                drive = _motorBody.zDrive;
            }
        }

        return drive;
    }

    private void SetDrive(in ArticulationDrive drive)
    {
        if (_motorBody.jointType.Equals(ArticulationJointType.RevoluteJoint))
        {
            _motorBody.xDrive = drive;
        }
        else if (_motorBody.jointType.Equals(ArticulationJointType.SphericalJoint))
        {
            if (!_motorBody.twistLock.Equals(ArticulationDofLock.LockedMotion))
            {
                _motorBody.xDrive = drive;
            }
            else if (!_motorBody.swingYLock.Equals(ArticulationDofLock.LockedMotion))
            {
                _motorBody.yDrive = drive;
            }
            else if (!_motorBody.swingZLock.Equals(ArticulationDofLock.LockedMotion))
            {
                _motorBody.zDrive = drive;
            }
        }
    }

    private void SetTargetVelocityAndForce(in float targetVelocity, in float targetForce)
    {
        // targetVelocity angular velocity in degrees per second.
        // Arccording to document(https://docs.unity3d.com/2020.2/Documentation/ScriptReference/ArticulationDrive.html)
        // F = stiffness * (currentPosition - target) - damping * (currentVelocity - targetVelocity).
        var drive = GetDrive();
        drive.damping = targetForce;
        drive.targetVelocity = targetVelocity;
        SetDrive(drive);
    }

    private void SetJointVelocity(in float velocity)
    {
        if (_motorBody != null)
        {
            var jointVelocity = _motorBody.jointVelocity;
            jointVelocity[0] = velocity;
            _motorBody.jointVelocity = jointVelocity;
        }
    }

    private float GetJointVelocity()
    {
        if (_motorBody == null)
        {
            return 0;
        }
        
        // calculate velocity using joint position is more accurate than joint velocity
        var jointPosition = _motorBody.jointPosition[0] * Mathf.Rad2Deg;
        var jointVelocity = (Mathf.DeltaAngle(_prevJointPosition, jointPosition) / Time.fixedDeltaTime);
        _prevJointPosition = jointPosition;

        return jointVelocity;
    }

    public class RapidChangeControl
    {
        private bool _directionSwitched = false;
        private const int _maxWaitCount = 30;
        private int _waitForStopCount = 0;

        public void SetDirectionSwitched(in bool switched)
        {
            // if (switched)
            // {
            //     Debug.Log(GetMotorName() + " - direction switched");
            // }

            _directionSwitched = switched;

            if (_directionSwitched)
            {
                _waitForStopCount = _maxWaitCount;
            }
        }

        public bool DirectionSwitched()
        {
            return _directionSwitched;
        }

        public void Wait()
        {
            if (_waitForStopCount-- <= 0)
            {
                SetDirectionSwitched(false);
            }
        }
    }

    public class MotorMotionFeedback
    {
        public float compensatingVelocityIncrease = 0.20f;
        public float compensatingVelocityDecrease = 0.60f;
        private bool _isRotating = false;
        private float _currentTwistAngularVelocity = 0;
        private float _targetTwistAngularVelocity = 0;
        private float _compensateValue = 0;
        public bool IsMotionRotating => _isRotating;

        public void SetMotionRotating(in bool enable)
        {
            _isRotating = enable;
        }

        public void SetRotatingVelocity(in float currentTwistAngularVelocity)
        {
            _currentTwistAngularVelocity = currentTwistAngularVelocity;
        }

        public void SetRotatingTargetVelocity(in float targetTwistAngularVelocity)
        {
            _targetTwistAngularVelocity = targetTwistAngularVelocity;
        }

        public bool IsTargetReached()
        {
            const float accuracy = 1000f;
            // Debug.Log(" is target reached: " + _currentTwistAngularVelocity + ", " + _targetTwistAngularVelocity);
            return ((int)Mathf.Abs(_currentTwistAngularVelocity * accuracy) >= (int)Mathf.Abs(_targetTwistAngularVelocity * accuracy));
        }

        public float Compensate()
        {
            if (IsMotionRotating)
            {
                if (IsTargetReached() == false)
                {
                    _compensateValue += compensatingVelocityIncrease;
                    // Debug.Log("_test: it is low speed, " + _currentTwistAngularVelocity + " < " + _targetTwistAngularVelocity);
                }
                else
                {
                    _compensateValue -= compensatingVelocityDecrease;

                    if (_compensateValue < 0)
                    {
                        _compensateValue = 0;
                    }
                }
            }
            else
            {
                _compensateValue = 0;
            }

            return _compensateValue;
        }
    }
}
