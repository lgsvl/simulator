/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafficLightVisuals : MonoBehaviour
{

    public Material green;
    public Material yellow;
    public Material red;
    public Renderer rend;
    public bool makeLight = false;
    private GameObject lightGameObject;

    void Awake()
    {
        if (makeLight)
        {
            lightGameObject = new GameObject("Traffic Light");
            lightGameObject.transform.SetParent(this.transform);
            lightGameObject.transform.rotation = this.transform.rotation * Quaternion.Euler(90, 0, 0);
            lightGameObject.transform.localPosition = new Vector3(0, 0, 0.0071f);

            Light lightComp = lightGameObject.AddComponent<Light>();
            lightComp.color = Color.red;
            lightComp.type = LightType.Spot;
            lightComp.spotAngle = 100;
            lightComp.range = 15;
            lightComp.intensity = 1.0f;
            lightComp.enabled = false;
        }

        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }

        rend.material = red;
    }

    public void Set(TrafLightState state)
    {
        switch (state)
        {
            case TrafLightState.RED:
                rend.material = red;
                break;
            case TrafLightState.YELLOW:
                rend.material = yellow;
                break;
            case TrafLightState.GREEN:
                rend.material = green;
                break;
        }

        if (makeLight)
        {
            switch (state)
            {
                case TrafLightState.RED:
                    lightGameObject.GetComponent<Light>().color = Color.red;
                    lightGameObject.transform.localPosition = new Vector3(0, 0, 0.0071f);
                    break;
                case TrafLightState.YELLOW:
                    lightGameObject.GetComponent<Light>().color = Color.yellow;
                    lightGameObject.transform.localPosition = new Vector3(0, 0, 0);
                    break;
                case TrafLightState.GREEN:
                    lightGameObject.GetComponent<Light>().color = Color.green;
                    lightGameObject.transform.localPosition = new Vector3(0, 0, -0.0086f);
                    break;
            }
        }
    }
}
