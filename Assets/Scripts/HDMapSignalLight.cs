/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class HDMapSignalLight : MapSignalLightBuilder
{
    public Vector3 boundOffsets = new Vector3(/*0, 0, -0.00055f*/);
    public Vector3 boundScale = new Vector3(/*0.0082f, 0.0243f, 0*/);

    public System.ValueTuple<Vector3, Vector3, Vector3, Vector3> Get2DBounds()
    {
        var matrix = transform.parent == null ? Matrix4x4.identity : transform.parent.localToWorldMatrix * Matrix4x4.TRS(transform.localPosition + boundOffsets, transform.localRotation, Vector3.Scale(transform.localScale, boundScale));

        float min = boundScale[0];
        int index = 0;
        for (int i = 0; i < 3; i++)
        {
            if (boundScale[i] < min)
            {
                min = boundScale[i];
                index = i;
            }
        }

        if (index == 0)
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0, 0.5f, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0, -0.5f, 0.5f)),
                matrix.MultiplyPoint(new Vector3(0, -0.5f, -0.5f)),
                matrix.MultiplyPoint(new Vector3(0, 0.5f, -0.5f))
                );
        }
        else if (index == 1)
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, 0.5f)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0, -0.5f)),
                matrix.MultiplyPoint(new Vector3(0.5f, 0, -0.5f))
                );
        }
        else
        {
            return new System.ValueTuple<Vector3, Vector3, Vector3, Vector3>(
                matrix.MultiplyPoint(new Vector3(0.5f, 0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0)),
                matrix.MultiplyPoint(new Vector3(0.5f, -0.5f, 0))
                );
        }
    }

    protected override void OnDrawGizmos()
    {
        if (!Map.MapTool.showMap) return;

        base.OnDrawGizmos();
        //Draw bounds
        Gizmos.matrix = transform.parent == null ? Matrix4x4.identity : transform.parent.localToWorldMatrix * Matrix4x4.TRS(transform.localPosition + boundOffsets, transform.localRotation, Vector3.Scale(transform.localScale, boundScale));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
