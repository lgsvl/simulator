/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class AgentZoneController : MonoBehaviour
{
    private LightLayerEnum BornLightLayer = LightLayerEnum.LightLayerDefault; //LightLayerEnum.LightLayer7;
    private uint CurrentLightMask = (uint)LightLayerEnum.LightLayerDefault;
    private Stack<uint> RenderMaskStack  = new Stack<uint>();

    private void Awake()
    {
        SetLightLayerMask((uint)BornLightLayer);
    }

    public void PushLightLayerMask()
    {
        RenderMaskStack.Push(CurrentLightMask);
    }

    public void PopLightLayerMask()
    {
        SetLightLayerMask(RenderMaskStack.Pop());
    }

    public void SetLightLayerMask(uint renderLightMask)
    {
        // Light layer is stored in the first 8 bit of the rendering layer mask.
        Component[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.renderingLayerMask = (r.renderingLayerMask & 0xFFFFFF00) | (byte)renderLightMask;
        }

        CurrentLightMask = renderLightMask;
    }
}
