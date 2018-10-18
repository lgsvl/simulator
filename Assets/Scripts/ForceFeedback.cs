/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VehicleInputController))]
[RequireComponent(typeof(SteeringWheelInputController))]
public class ForceFeedback : MonoBehaviour
{
    private SteeringWheelInputController steerwheel;
    private VehicleInputController vehicleInputContrl;
    public WheelCollider[] wheels;

    public AnimationCurve damperCurve;
    public float weightIntensity = -5f;
    public float tireWidth = 0.25f;

    public float springSaturation = 0.4f;
    public float springCoeff = 0.05f;
    public int damperAmount = 3000;

    private Rigidbody rb;

    void Start()
    {
        vehicleInputContrl = GetComponent<VehicleInputController>();
        steerwheel = GetComponent<SteeringWheelInputController>();
        rb = GetComponent<Rigidbody>();
    }

    float raw = 0f;
    int calculated = 0;

    float RPMToAngularVel(float rpm)
    {
        return rpm * 2 * Mathf.PI / 60f;
    }

    void Update()
    {
        float selfAlignmentTorque = 0f;
        foreach (var wheel in wheels)
        {
            if (wheel.isGrounded)
            {
                WheelHit hit;
                wheel.GetGroundHit(out hit);

                Vector3 left = hit.point - (hit.sidewaysDir * tireWidth * 0.5f);
                Vector3 right = hit.point + (hit.sidewaysDir * tireWidth * 0.5f);

                Vector3 leftTangent = rb.GetPointVelocity(left);
                leftTangent -= Vector3.Project(leftTangent, hit.normal);

                Vector3 rightTangent = rb.GetPointVelocity(right);
                rightTangent -= Vector3.Project(rightTangent, hit.normal);

                float slipDifference = Vector3.Dot(hit.forwardDir, rightTangent) - Vector3.Dot(hit.forwardDir, leftTangent);

                selfAlignmentTorque += (0.5f * weightIntensity * slipDifference) / 2f;

            }
        }

        float forceFeedback = selfAlignmentTorque;

        if (steerwheel != null && steerwheel.available)
        {
            if (!vehicleInputContrl.selfDriving || steerwheel.autonomousBehavior == SteerWheelAutonomousFeedbackBehavior.InputAndOutputWithRoadFeedback)
            {
                var steerwheel_ffb = steerwheel as IForceFeedback;
                steerwheel_ffb.SetConstantForce((int)(forceFeedback * 10000f));
                steerwheel_ffb.SetSpringForce(Mathf.RoundToInt(springSaturation * Mathf.Abs(forceFeedback) * 10000f), Mathf.RoundToInt(springCoeff * 10000f));
                steerwheel_ffb.SetDamperForce(damperAmount);
            }        
        }
    }
}
