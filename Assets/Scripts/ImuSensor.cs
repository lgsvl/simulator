/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using UnityEngine;

//Apollo only
public class ImuSensor : MonoBehaviour, Ros.IRosClient
{
    private static readonly string ImuTopic = "/apollo/sensor/gnss/imu";
    private static readonly string ApolloIMUOdometryTopic = "/apollo/sensor/gnss/corrected_imu";
    private Vector3 lastVelocity;

    Ros.Bridge Bridge;
    uint seq;

    public string FrameId = "/imu";
    public Rigidbody mainRigidbody;
    public GameObject Target;
    public bool PublishMessage = false;
    
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
        Bridge.AddPublisher<Ros.CorrectedImu>(ApolloIMUOdometryTopic);
        seq = 0;
    }

    public void FixedUpdate()
    {
        if (Bridge == null || Bridge.Status != Ros.Status.Connected || !PublishMessage)
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
        double measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;
        float measurement_span = (float)Time.fixedDeltaTime;

        // Debug.Log(measurement_time + ", " + measurement_span);
        // Debug.Log("Linear Acceleration: " + linear_acceleration.x.ToString("F1") + ", " + linear_acceleration.y.ToString("F1") + ", " + linear_acceleration.z.ToString("F1"));
        // Debug.Log("Angular Velocity: " + angular_velocity.x.ToString("F1") + ", " + angular_velocity.y.ToString("F1") + ", " + angular_velocity.z.ToString("F1"));

        Bridge.Publish(ImuTopic, new Ros.Imu()
        {
            header = new Ros.ApolloHeader()
            {
                timestamp_sec = measurement_time
            },
            measurement_time = measurement_time,
            measurement_span = measurement_span,
            linear_acceleration = linear_acceleration,
            angular_velocity = angular_velocity
        });

        var angles = Target.transform.eulerAngles;
        float roll = -angles.z;
        float pitch = -angles.x;
        float yaw = angles.y;

        var apolloIMUMessage = new Ros.CorrectedImu()
        {
            header = new Ros.ApolloHeader()
            {
                timestamp_sec = measurement_time
            },

            imu = new Ros.ApolloPose()
            {
                // Position of the vehicle reference point (VRP) in the map reference frame.
                // The VRP is the center of rear axle.
                position = new Ros.PointENU(),

                // A quaternion that represents the rotation from the IMU coordinate
                // (Right/Forward/Up) to the
                // world coordinate (East/North/Up).
                orientation = new Ros.ApolloQuaternion(),

                // Linear velocity of the VRP in the map reference frame.
                // East/north/up in meters per second.
                linear_velocity = new Ros.Point3D(),

                // Linear acceleration of the VRP in the map reference frame.
                // East/north/up in meters per second.
                linear_acceleration = linear_acceleration,

                // Angular velocity of the vehicle in the map reference frame.
                // Around east/north/up axes in radians per second.
                angular_velocity = angular_velocity,

                // Heading
                // The heading is zero when the car is facing East and positive when facing North.
                heading = yaw,  // not used ??

                // Linear acceleration of the VRP in the vehicle reference frame.
                // Right/forward/up in meters per square second.
                linear_acceleration_vrf = new Ros.Point3D(),

                // Angular velocity of the VRP in the vehicle reference frame.
                // Around right/forward/up axes in radians per second.
                angular_velocity_vrf = new Ros.Point3D(),

                // Roll/pitch/yaw that represents a rotation with intrinsic sequence z-x-y.
                // in world coordinate (East/North/Up)
                // The roll, in (-pi/2, pi/2), corresponds to a rotation around the y-axis.
                // The pitch, in [-pi, pi), corresponds to a rotation around the x-axis.
                // The yaw, in [-pi, pi), corresponds to a rotation around the z-axis.
                // The direction of rotation follows the right-hand rule.
                euler_angles = new Ros.Point3D()
                {
                    x = roll * 0.01745329252,
                    y = pitch * 0.01745329252,
                    z = yaw * 0.01745329252
                }
            }
        };

        Bridge.Publish(ApolloIMUOdometryTopic, apolloIMUMessage);
    }
}
