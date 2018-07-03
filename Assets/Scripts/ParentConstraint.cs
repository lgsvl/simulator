/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class ParentConstraint : MonoBehaviour
{
    public enum UpdateMethod
    {
        Update,
        LateUpdate,
        FixedUpdate
    }
    public UpdateMethod updateMethod = UpdateMethod.FixedUpdate;
    public Transform target;

    private Vector3 localposOffset;
    private Quaternion rotOffset;

    void Start()
    {
        localposOffset = target.transform.InverseTransformVector(transform.position - target.position);
        rotOffset = transform.rotation * Quaternion.Inverse(target.rotation);
    }

    void FixedUpdate ()
    {
        transform.rotation = rotOffset * target.rotation;
        transform.position = target.position + target.transform.TransformVector(localposOffset);
    }
}
