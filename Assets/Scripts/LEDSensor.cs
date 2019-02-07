/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LEDSensor : MonoBehaviour, Ros.IRosClient
{
    public string ledTopicName = "/central_controller/effects";
    private Ros.Bridge Bridge;
    private bool isEnabled = false;
    private bool isFirstEnabled = true;

    public List<Renderer> ledMats = new List<Renderer>();
    private List<Light> ledLights = new List<Light>();
    private Material sharedMat;
    public float rate = 0.5f;
    private float offset = 0f;
    public Color color = new Color();

    private void Awake()
    {
        ledLights.AddRange(GetComponentsInChildren<Light>());
        sharedMat = ledMats[0].sharedMaterial;
        AddUIElement();
    }

    private void Update()
    {
        if (isEnabled)
        {
            sharedMat.EnableKeyword("_EMISSION");
            offset = Time.time * rate;
            sharedMat.SetTextureOffset("_MainTex", new Vector2(0, offset));
            sharedMat.SetColor("_EmissionColor", color * 2f);
        }
        else
        {
            sharedMat.DisableKeyword("_EMISSION");
            sharedMat.SetColor("_EmissionColor", Color.white);
        }
        
        foreach (var item in ledLights)
        {
            item.color = color;
            item.enabled = isEnabled;
        }
    }

    private void OnDisable()
    {
        sharedMat.SetColor("_EmissionColor", Color.white);
        sharedMat.DisableKeyword("_EMISSION");
    }

    public void Enable(bool enabled)
    {
        isEnabled = enabled;
        if (isEnabled && isFirstEnabled)
        {
            isFirstEnabled = false;
            AgentSetup agentSetup = GetComponentInParent<AgentSetup>();
            if (agentSetup != null && agentSetup.NeedsBridge != null)
            {
                agentSetup.AddToNeedsBridge(this);
            }
        }
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.LED>(ledTopicName);
    }

    private void AddUIElement()
    {
        var ledCheckbox = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox("ToggleLED", "Enable LED:", isEnabled);
        ledCheckbox.onValueChanged.AddListener(x => Enable(x));
    }
}
