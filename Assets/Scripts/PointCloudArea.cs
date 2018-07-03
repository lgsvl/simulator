/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;

public class PointCloudArea : MonoBehaviour
{
    [Range(1.0f, 100.0f)]
    public float densityScaler;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale * 1.0f);
    }

    public bool Contains(Vector3 point)
    {
        var p = transform.InverseTransformPoint(point);
        return Mathf.Abs(p.x) < 0.5f && Mathf.Abs(p.y) < 0.5f && Mathf.Abs(p.z) < 0.5f;
    }
}
