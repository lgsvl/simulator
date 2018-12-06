/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEngine;

public class WheelController : MonoBehaviour
{
    // Axis to rotate the object around
    public Transform rotationAxis;

    public VehicleController vc_input;

    public Vector3 initialLocalPos;
    public Quaternion initialLocalRot;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    void Update()
    {
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;

        var angle = -vc_input.steerInput * 450.0f;
        transform.RotateAround(rotationAxis.position, rotationAxis.up, -angle);
    }
}