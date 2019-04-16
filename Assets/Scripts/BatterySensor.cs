/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BatterySensor : MonoBehaviour, Comm.BridgeClient
{
    public string batteryTopicName = "/central_controller/soc";
    private Comm.Bridge Bridge;
    public float batteryCurrentCharge = 0f;
    private float batteryMax = 100f;
    private float batteryDischargeRate = 0.1f;
    private Slider batterySlider;

    private void Awake()
    {
        AddUIElement();
        SetBattery(batteryMax);
    }

    private void Update()
    {
        batteryCurrentCharge -= Time.deltaTime * batteryDischargeRate;
        SetBatterySlider();
    }

    public void GetSensors(List<Component> sensors)
    {
        sensors.Add(this);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            Bridge.AddService<Ros.Srv.SetStateOfCharge, Ros.Srv.SetStateOfChargeResponse>(batteryTopicName, msg =>
            {
                SetBattery(msg.data);
                SetBatterySlider();
                return new Ros.Srv.SetStateOfChargeResponse() { success = true, message = "message" };
            });
        };
    }

    public void OnRosConnected()
    {

    }

    private void SetBattery(float charge)
    {
        batteryCurrentCharge = charge;
    }

    private void SetBatterySlider()
    {
        batterySlider.value = batteryCurrentCharge;
    }

    private void AddUIElement() // TODO combine with tweakables prefab for all sensors issues on start though
    {
        batterySlider = GetComponentInParent<UserInterfaceTweakables>().AddFloatSlider("Battery", "Battery: ", 0f, batteryMax, batteryMax);
        batterySlider.onValueChanged.AddListener(x => SetBattery(x));
    }
}
