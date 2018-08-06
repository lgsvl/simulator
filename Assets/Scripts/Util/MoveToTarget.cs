/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class MoveToTarget : MonoBehaviour {

    public float speed = 2f;
    public Vector3 target = Vector3.zero;
    public bool local = false;
    public bool hasFollowTarget = false;
    public Transform followTarget;
    public bool updateRotation = false;
    public void SetFollowTarget(Transform t)
    {
        updateRotation = false;
        followTarget = t;
        hasFollowTarget = true;
    }

    public void SetTarget(Vector3 newTarget)
    {
        hasFollowTarget = false;
        target = newTarget;
    }

    public void ForceTarget(Vector3 newTarget)
    {
        target = newTarget;
        transform.position = newTarget;
    }

    void Update()
    {
        if(hasFollowTarget)
        {
            target = followTarget.position;
            if(updateRotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, speed * Time.deltaTime);
        }

        if(local && !hasFollowTarget)
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * speed);
        else
            transform.position = Vector3.Lerp(transform.position, target,  Time.deltaTime * speed);
    }
    

}
