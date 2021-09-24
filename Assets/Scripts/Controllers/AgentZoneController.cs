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
    private LightLayerTrigger CurrentLightMask = null;
    private Stack<LightLayerTrigger> RenderMaskStack  = new Stack<LightLayerTrigger>();

    private void Awake()
    {
        SetLightLayerMask(null);
        
        var triggers = FindObjectsOfType<LightLayerTrigger>();
        foreach (var trigger in triggers)
        {
            var boxCollider = trigger.GetComponent<BoxCollider>();
            if (boxCollider == null)
                continue;

            if (IsPointWithinCollider(transform.position, boxCollider))
            {
                PushLightLayerMask();
                SetLightLayerMask(trigger);
            }
        }
    }

    private bool IsPointWithinCollider(Vector3 worldPoint, BoxCollider boxCollider)
    {
        var point = boxCollider.transform.InverseTransformPoint(worldPoint) - boxCollider.center;
        var size = boxCollider.size;
        var halfX = size.x * 0.5f;
        var halfY = size.y * 0.5f;
        var halfZ = size.z * 0.5f;
        return point.x < halfX && point.x > -halfX && 
               point.y < halfY && point.y > -halfY && 
               point.z < halfZ && point.z > -halfZ;
    }

    public void PushLightLayerMask()
    {
        if (RenderMaskStack.Contains(CurrentLightMask))
            return;

        RenderMaskStack.Push(CurrentLightMask);
    }

    public void PopLightLayerMask()
    {
        SetLightLayerMask(RenderMaskStack.Pop());
    }

    public void SetLightLayerMask(LightLayerTrigger trigger)
    {
        var renderLightMask = trigger != null ? trigger.LightLayer : LightLayerEnum.LightLayerDefault;
        // Light layer is stored in the first 8 bit of the rendering layer mask.
        Component[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.renderingLayerMask = (r.renderingLayerMask & 0xFFFFFF00) | (byte)renderLightMask;
        }

        CurrentLightMask = trigger;
    }
}
