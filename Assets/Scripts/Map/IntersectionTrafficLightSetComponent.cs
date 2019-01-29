/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTrafficLightSetComponent : MonoBehaviour
{
    public List<Renderer> lightRenderers = new List<Renderer>();
    public TrafficLightSetState currentState = TrafficLightSetState.None;
    public MapStopLineSegmentBuilder stopline;

    public void SetLightRendererData()
    {
        lightRenderers.AddRange(transform.GetComponentsInChildren<Renderer>());
    }

    public void SetLightColor(TrafficLightSetState state, Material mat)
    {
        currentState = state;
        if (stopline != null)
            stopline.currentState = state;
        foreach (var item in lightRenderers)
        {
            item.material = mat;
        }
    }
}
