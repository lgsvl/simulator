/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopLightSensor : MonoBehaviour, Ros.IRosClient
{
    public string ledTopicName = "/central_controller/flash";
    private Ros.Bridge Bridge;
    private bool isEnabled = false;
    private bool isFirstEnabled = true;

    public Renderer lightRenderer;
    private Light topLight;
    private Color offColor = Color.white;
    private Color onColor = new Color(1f, 0.7f, 0f);
    private void Awake()
    {
        topLight = GetComponentInChildren<Light>();
        AddUIElement();
        SetTopLightMode(false);
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
                lightRenderer.material.color = onColor;
                yield return new WaitForSecondsRealtime(0.05f);
                ToggleTopLight(true);
                yield return new WaitForSecondsRealtime(0.05f);
                ToggleTopLight(false);
            }
            lightRenderer.material.color = offColor;
        }
    }

    private void ParseMsg(int msg)
    {
        isEnabled = msg == 0 ? false : true;
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        //Bridge.AddPublisher<Ros.LED>(ledTopicName);
    }

    private void AddUIElement() // TODO combine with tweakables prefab for all sensors issues on start though
    {
        var ledModeDropdown = GetComponentInParent<UserInterfaceTweakables>().AddCheckbox("ToggleTopLight", "Enable Top Light: ", isEnabled);
        ledModeDropdown.onValueChanged.AddListener(x => SetTopLightMode(x));
    }
}
