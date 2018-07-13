using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImuSensor : MonoBehaviour, Ros.IRosClient
{
    private static readonly string ImuTopic = "/apollo/sensor/gnss/imu";
    private Vector3 lastVelocity;

    Ros.Bridge Bridge;
    uint seq;

    public string FrameId = "/imu";
    public Rigidbody mainRigidbody;
    
    private void Start()
    {
        lastVelocity = Vector3.zero;
    }
    
    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        Bridge.AddPublisher<Ros.Imu>(ImuTopic);
        seq = 0;
    }

    public void FixedUpdate()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected)
        {
            return;
        }

        Vector3 currVelocity = transform.InverseTransformDirection(mainRigidbody.velocity);
        Vector3 acceleration = (currVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currVelocity;

        var linear_acceleration = new Ros.Point3D()
        {
            x = acceleration.x,
            y = acceleration.z,
            z = -Physics.gravity.y
        };

        Vector3 angularVelocity = mainRigidbody.angularVelocity;
        var angular_velocity = new Ros.Point3D()
        {
            x = 0.0,
            y = 0.0,
            z = -angularVelocity.y
        };

        System.DateTime GPSepoch = new System.DateTime(1980, 1, 6, 0, 0, 0, System.DateTimeKind.Utc);
        double measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds;
        float measurement_span = (float)Time.fixedDeltaTime;

        // Debug.Log(measurement_time + ", " + measurement_span);
        // Debug.Log("Linear Acceleration: " + linear_acceleration.x.ToString("F1") + ", " + linear_acceleration.y.ToString("F1") + ", " + linear_acceleration.z.ToString("F1"));
        // Debug.Log("Angular Velocity: " + angular_velocity.x.ToString("F1") + ", " + angular_velocity.y.ToString("F1") + ", " + angular_velocity.z.ToString("F1"));

        Bridge.Publish(ImuTopic, new Ros.Imu()
        {
            header = new Ros.ApolloHeader()
            {
                timestamp_sec = Ros.Time.Now().secs
            },
            measurement_time = measurement_time,
            measurement_span = measurement_span,
            linear_acceleration = linear_acceleration,
            angular_velocity = angular_velocity
        });
    }
}
