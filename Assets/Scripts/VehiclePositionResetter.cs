using UnityEngine;

public class VehiclePositionResetter : MonoBehaviour, Ros.IRosClient
{
    Ros.Bridge Bridge;

    public GpsDevice GpsDevice;
    public string ResetTopic = "/simulator/reset";

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.Subscribe<Ros.Vector3>(ResetTopic, msg =>
        {
            var position = GpsDevice.GetPosition(msg.x, msg.y);
            position.y = 10.0f; // TODO: raycast to find ground?
            var rotation = Quaternion.AngleAxis((float)msg.z * Mathf.Rad2Deg, Vector3.up);
            transform.SetPositionAndRotation(position, rotation);
        });
    }
}
