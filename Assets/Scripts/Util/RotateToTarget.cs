/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class RotateToTarget : MonoBehaviour {

    public float target = 0f;
    public AnimationCurve rotateCurve;
    public float rotateTime = 1.5f;
    private Quaternion startPos;
    private float startTime;

    public bool local = false;

    public void SetTarget(Transform ntarget)
    {
        SetTarget(ntarget.localEulerAngles.y);
    }

    public void SetTarget(float newTarget)
    {
        target = newTarget;
        if(local)
            startPos = transform.localRotation;
        else
            startPos = transform.rotation;
        startTime = Time.time;
    }

    public void ForceTarget(float newTarget)
    {       
        target = newTarget;
        startPos = Quaternion.Euler(0, target, 0f);
        if(local)
            transform.localRotation = Quaternion.Euler(0f, newTarget, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, newTarget, 0f);
    }

	void Update () {
        if(local)
            transform.localRotation = Quaternion.Lerp(startPos, Quaternion.Euler(0, target, 0f), rotateCurve.Evaluate((Time.time - startTime) / rotateTime));
        else
            transform.rotation = Quaternion.Lerp(startPos, Quaternion.Euler(0, target, 0f), rotateCurve.Evaluate((Time.time - startTime)/rotateTime));
	}
}
