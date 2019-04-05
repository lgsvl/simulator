/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

public class VehiclePositionResetter : MonoBehaviour, Comm.BridgeClient
{
    Comm.Bridge Bridge;

    public GpsDevice GpsDevice;
    public MapOrigin MapOrigin;
    public string ResetTopic = "/simulator/reset";

    void Start()
    {
        MapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();
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
            Bridge.AddReader<Ros.Vector3>(ResetTopic, msg =>
            {
                var position = MapOrigin.FromNorthingEasting(msg.y, msg.x);

                int mask = 1 << LayerMask.NameToLayer("Ground And Road");
                RaycastHit hit;
                if (Physics.Raycast(position + new Vector3(0, 100, 0), new Vector3(0, -1, 0), out hit, Mathf.Infinity, mask))
                {
                    position = hit.point;
                    position.y += 0.01f;
                }
                else
                {
                    position.y += 20.0f;
                }

                var angle = (float)msg.z * Mathf.Rad2Deg - MapOrigin.Angle;
                var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                // reset position, rotation, velocity and angular velocity
                GpsDevice.Agent.GetComponent<VehicleInputController>().controller.ResetSavedPosition(position, rotation);

            });
        };
    }
}
