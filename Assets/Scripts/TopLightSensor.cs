/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopLightSensor : MonoBehaviour, Comm.BridgeClient
{
    public string topLightTopicName = "/central_controller/flash";
    private Comm.Bridge Bridge;
    private bool isEnabled = false;

    public Renderer lightRenderer;
    private Light topLight;
    private Color offColor = new Color(1f, 1f, 1f, 0.5f);
    private Color onColor = new Color(1f, 0.7f, 0f, 0.5f);
    private void Awake()
    {
        topLight = GetComponentInChildren<Light>();
        AddUIElement();
        ToggleTopLight(false);
        lightRenderer.material.color = offColor;
    }

    private void SetTopLightMode(bool enabled)
    {
        isEnabled = enabled;
        
        if (enabled == false)
        {
            StopAllCoroutines();
            ToggleTopLight(enabled);
            lightRenderer.material.color = offColor;
        }
        else
        {
            StartCoroutine(BlinkTopLight());
        }
    }

    private void ToggleTopLight(bool enabled)
    {
        topLight.enabled = enabled ? true : false;
        if (enabled)
            lightRenderer.material.EnableKeyword("_EMISSION");
        else
            lightRenderer.material.DisableKeyword("_EMISSION");
        
    }

    private IEnumerator BlinkTopLight()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.3f); // fixed by manufacturer
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSecondsRealtime(0.05f);
                ToggleTopLight(true);
                lightRenderer.material.color = onColor;
                yield return new WaitForSecondsRealtime(0.05f);
                ToggleTopLight(false);
                lightRenderer.material.color = offColor;
            }
            lightRenderer.material.color = offColor;
        }
    }

    private void ParseMsg(int msg)
    {
        isEnabled = msg == 0 ? false : true;
    }

    public void GetSensors(List<Component> sensors)
    {
        // this is not a sensor
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            Bridge.AddService<Ros.Srv.SetEffect, Ros.Srv.SetEffectResponse>(topLightTopicName, msg =>
            {
                switch(msg.data)
                {
                    case 0:
                        SetTopLightMode(false);
                        break;
                    case 1:
                        SetTopLightMode(true);
                        break;
                }
                return new Ros.Srv.SetEffectResponse() { success = true, message = "message" };
            });
        };
    }

    private void AddUIElement() // TODO combine with tweakables prefab for all sensors issues on start though
    {
        var ledModeDropdown = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox("ToggleTopLight", "Enable Top Light: ", isEnabled);
        ledModeDropdown.onValueChanged.AddListener(x => SetTopLightMode(x));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
