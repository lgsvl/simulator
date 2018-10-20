/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class VectorMapPoleBuilder : MonoBehaviour
{
    public List<MapSignalLightBuilder> signalLights;
    public float length = 6.75f;

    protected virtual void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        Vector3 dir = transform.forward;

        Map.Draw.Gizmos.DrawArrow(pos, pos + dir * length, Color.white, Map.Autoware.VectorMapTool.ARROWSIZE, arrowPositionRatio:1f);
    }
}
