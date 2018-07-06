/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

public class CamSmoothFollow : MonoBehaviour
{
    public Transform targetPositionTransform;
    public Transform targetObject;
    public float targetDistance = 5f;
    public float targetHeight = 1.7f;
    public float lookAbove = 0.85f;
    public float heightDamping = 2f;
    public float rotationDamping = 4.5f;
    public float positionDamping = 5f;

    public float currentHeight;
    public float currentRotation;
    public Vector3 currentPosition;

    public void OnEnable() {
        currentHeight = targetPositionTransform.position.y + targetHeight;
        currentRotation = targetObject.rotation.eulerAngles.y;
        currentPosition = targetPositionTransform.position;
    }

    public void FixedUpdate() {
        if (targetObject == null || targetPositionTransform == null) {
            return;
        }
        currentHeight = Mathf.Lerp(currentHeight, targetPositionTransform.position.y + targetHeight, Time.deltaTime * heightDamping);
        currentRotation = Mathf.LerpAngle(currentRotation, targetObject.rotation.eulerAngles.y, Time.deltaTime * rotationDamping);
        var dot = Vector3.Dot(targetObject.position - targetPositionTransform.position, targetObject.forward);
        var newPos = targetPositionTransform.position - (Quaternion.Euler(0f, currentRotation, 0f) * (dot > 1 ? Vector3.forward : Vector3.back) * targetDistance);
        newPos.y = currentHeight;
        currentPosition = Vector3.Lerp(currentPosition, newPos, Time.deltaTime * positionDamping);
        transform.position = currentPosition;

        var lookTarget = Quaternion.LookRotation(((targetObject.position + Vector3.up * lookAbove) - transform.position).normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookTarget, Time.deltaTime * 3);
    }
}
