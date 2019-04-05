using System.Collections.Generic;
using UnityEngine;

public class VehiclePositionResetter : MonoBehaviour, Comm.BridgeClient
{
    Comm.Bridge Bridge;

    public GpsDevice GpsDevice;
    public string ResetTopic = "/simulator/reset";

    public void GetSensors(List<Component> sensors)
    {
        // this is not a sensor
    }


    Vector3 GetPosition(ROSTargetEnvironment targetEnv, double easting, double northing)
    {
        MapOrigin mapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();

        if (targetEnv == ROSTargetEnvironment.APOLLO || targetEnv == ROSTargetEnvironment.APOLLO35)
        {
            easting += 500000;
        }
        easting -= mapOrigin.OriginEasting;
        northing -= mapOrigin.OriginNorthing;

        float x = (float)easting;
        float z = (float)northing;

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
            return new Vector3(x, 0, z);
        return Quaternion.Euler(0f, -mapOrigin.Angle, 0f) * new Vector3(x, 0, z);
    }

    public void OnBridgeAvailable(Comm.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.OnConnected += () =>
        {
            Bridge.AddReader<Ros.Vector3>(ResetTopic, msg =>
            {
                var position = GetPosition(GpsDevice.targetEnv, msg.x, msg.y);

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
                var angle = (float)msg.z * Mathf.Rad2Deg - GpsDevice.Angle;
                var rotation = Quaternion.AngleAxis(angle, Vector3.up);
                // reset position, rotation, velocity and angular velocity
                GpsDevice.Agent.GetComponent<VehicleInputController>().controller.ResetSavedPosition(position, rotation);

            });
        };
    }
}
