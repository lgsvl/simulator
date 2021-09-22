/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class LightLayerTrigger : MonoBehaviour
{
    public LightLayerEnum LightLayer = LightLayerEnum.LightLayerDefault;

    void OnTriggerEnter(Collider other)
    {
        var agentZoneController = other.gameObject.GetComponent<AgentZoneController>();

        if (agentZoneController != null)
        {
            agentZoneController.PushLightLayerMask();
            agentZoneController.SetLightLayerMask(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var agentZoneController = other.gameObject.GetComponent<AgentZoneController>();

        if (agentZoneController != null)
        {
            agentZoneController.PopLightLayerMask();
        }
    }
}
