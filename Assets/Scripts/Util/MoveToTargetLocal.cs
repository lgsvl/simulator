/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class MoveToTargetLocal : MonoBehaviour
{

    public float speed = 2f;
    public Vector3 target = Vector3.zero;

    public void SetTarget(Vector3 newTarget)
    {
        target = newTarget;
    }

    public void ForceTarget(Vector3 newTarget)
    {
        target = newTarget;
        transform.localPosition = newTarget;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * speed);
    }


}
