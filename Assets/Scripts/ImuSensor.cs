/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ImuSensor : MonoBehaviour, Comm.BridgeClient
{
    public string SensorName = "IMU";
    public string ImuTopic = "/apollo/sensor/gnss/imu";
    public string ImuFrameId = "/imu";
    public string OdometryTopic = "/odometry";
    public string OdometryFrameId = "/odom";
    public string OdometryChildFrameId = "/none";
    private static readonly string ApolloIMUOdometryTopic = "/apollo/sensor/gnss/corrected_imu";
    public string ResetOdometry = "/reset_odom";
    public ROSTargetEnvironment TargetRosEnv;
    private Vector3 lastVelocity;
    private Vector3 odomPosition = new Vector3(0f, 0f, 0f);

    Comm.Bridge Bridge;

    Comm.Writer<Ros.Apollo.Imu> ApolloWriterImu;
    Comm.Writer<Ros.CorrectedImu> ApolloWriterCorrectedImu;
    Comm.Writer<Apollo.Drivers.Gnss.Imu> Apollo35WriterImu;
    Comm.Writer<Apollo.Localization.CorrectedImu> Apollo35WriterCorrectedImu;

    Comm.Writer<Ros.Imu> AutowareWriterImu;
    Comm.Writer<Ros.Odometry> AutowareWriterOdometry;
    public Rigidbody mainRigidbody;
    public GameObject Target;
    private GameObject Agent;
    public bool PublishMessage = false;

    bool isEnabled = false;
    uint Sequence;

    private void Awake()
    {
        AddUIElement();
    }

    private void Start()
    {
        lastVelocity = Vector3.zero;
    }

    public void Enable(bool enabled)
    {
        isEnabled = enabled;
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
            if (TargetRosEnv == ROSTargetEnvironment.APOLLO)
            {
                ApolloWriterImu = Bridge.AddWriter<Ros.Apollo.Imu>(ImuTopic);
                ApolloWriterCorrectedImu = Bridge.AddWriter<Ros.CorrectedImu>(ApolloIMUOdometryTopic);
            }
            else if (TargetRosEnv == ROSTargetEnvironment.APOLLO35)
            {
                Apollo35WriterImu = Bridge.AddWriter<Apollo.Drivers.Gnss.Imu>(ImuTopic);
                Apollo35WriterCorrectedImu = Bridge.AddWriter<Apollo.Localization.CorrectedImu>(ApolloIMUOdometryTopic);
            }
            else if (TargetRosEnv == ROSTargetEnvironment.AUTOWARE || TargetRosEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1)
            {
                AutowareWriterImu = Bridge.AddWriter<Ros.Imu>(ImuTopic);
                AutowareWriterOdometry = Bridge.AddWriter<Ros.Odometry>(OdometryTopic);
                Bridge.AddService<Ros.Srv.Empty, Ros.Srv.Empty>(ResetOdometry, msg =>
                {
                    odomPosition = new Vector3(0f, 0f, 0f);
                    return new Ros.Srv.Empty();
                });
            }
        };
    }

    public void FixedUpdate()
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected || !PublishMessage || !isEnabled)
        {
            return;
        }

        Vector3 currVelocity = transform.InverseTransformDirection(mainRigidbody.velocity);
        Vector3 acceleration = (currVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currVelocity;

        Vector3 angularVelocity = mainRigidbody.angularVelocity;
        

        System.DateTime GPSepoch = new System.DateTime(1980, 1, 6, 0, 0, 0, System.DateTimeKind.Utc);
        double measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;
        float measurement_span = (float)Time.fixedDeltaTime;

        // Debug.Log(measurement_time + ", " + measurement_span);
        // Debug.Log("Linear Acceleration: " + linear_acceleration.x.ToString("F1") + ", " + linear_acceleration.y.ToString("F1") + ", " + linear_acceleration.z.ToString("F1"));
        // Debug.Log("Angular Velocity: " + angular_velocity.x.ToString("F1") + ", " + angular_velocity.y.ToString("F1") + ", " + angular_velocity.z.ToString("F1"));
        var angles = Target.transform.eulerAngles;
        float roll = -angles.z;
        float pitch = -angles.x;
        float yaw = -angles.y;
        Quaternion orientation_unity = Quaternion.Euler(roll, pitch, yaw);
        System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;

        // for odometry frame position
        odomPosition.x += currVelocity.z * Time.fixedDeltaTime * Mathf.Cos(yaw * (Mathf.PI / 180.0f));
        odomPosition.y += currVelocity.z * Time.fixedDeltaTime * Mathf.Sin(yaw * (Mathf.PI / 180.0f));

        acceleration += transform.InverseTransformDirection(Physics.gravity);

        if (TargetRosEnv == ROSTargetEnvironment.APOLLO)
        {
            var linear_acceleration = new Ros.Point3D()
            {
                x = acceleration.x,
                y = acceleration.z,
                z = acceleration.y,
            };
            var angular_velocity = new Ros.Point3D()
            {
                x = -angularVelocity.x,
                y = -angularVelocity.z,
                z = -angularVelocity.y,
            };

            ApolloWriterImu.Publish(new Ros.Apollo.Imu()
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
                    // position = new Ros.PointENU(),

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the
                    // world coordinate (East/North/Up).
                    // orientation = new Ros.ApolloQuaternion(),

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    // linear_velocity = new Ros.Point3D(),

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
                    // linear_acceleration_vrf = new Ros.Point3D(),

                    // Angular velocity of the VRP in the vehicle reference frame.
                    // Around right/forward/up axes in radians per second.
                    // angular_velocity_vrf = new Ros.Point3D(),

                    // Roll/pitch/yaw that represents a rotation with intrinsic sequence z-x-y.
                    // in world coordinate (East/North/Up)
                    // The roll, in (-pi/2, pi/2), corresponds to a rotation around the y-axis.
                    // The pitch, in [-pi, pi), corresponds to a rotation around the x-axis.
                    // The yaw, in [-pi, pi), corresponds to a rotation around the z-axis.
                    // The direction of rotation follows the right-hand rule.
                    euler_angles = new Ros.Point3D()
                    {
                        x = roll * Mathf.Deg2Rad,
                        y = pitch * Mathf.Deg2Rad,
                        z = yaw * Mathf.Deg2Rad
                    }
                }
            };

            ApolloWriterCorrectedImu.Publish(apolloIMUMessage);
        }

        else if (TargetRosEnv == ROSTargetEnvironment.APOLLO35)
        {
            var linear_acceleration = new Apollo.Common.Point3D()
            {
                X = acceleration.x,
                Y = acceleration.z,
                Z = acceleration.y, 
            };
            var angular_velocity = new Apollo.Common.Point3D()
            {
                X = -angularVelocity.x,
                Y = -angularVelocity.z,
                Z = -angularVelocity.y,
            };

             Apollo35WriterImu.Publish(new Apollo.Drivers.Gnss.Imu()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = measurement_time
                },
                MeasurementTime = measurement_time,
                MeasurementSpan = measurement_span,
                LinearAcceleration = linear_acceleration,
                AngularVelocity = angular_velocity
            });

            var apolloIMUMessage = new Apollo.Localization.CorrectedImu()
            {
                Header = new Apollo.Common.Header()
                {
                    TimestampSec = measurement_time                  
                },
                Imu = new Apollo.Localization.Pose()
                {
                    // Position of the vehicle reference point (VRP) in the map reference frame.
                    // The VRP is the center of rear axle.
                    // position = new Ros.PointENU(),

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the
                    // world coordinate (East/North/Up).
                    // orientation = new Ros.ApolloQuaternion(),

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    // linear_velocity = new Ros.Point3D(),

                    // Linear acceleration of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    LinearAcceleration = linear_acceleration,

                    // Angular velocity of the vehicle in the map reference frame.
                    // Around east/north/up axes in radians per second.
                    AngularVelocity = angular_velocity,

                    // Heading
                    // The heading is zero when the car is facing East and positive when facing North.
                    Heading = yaw,  // not used ??

                    // Linear acceleration of the VRP in the vehicle reference frame.
                    // Right/forward/up in meters per square second.
                    // linear_acceleration_vrf = new Ros.Point3D(),

                    // Angular velocity of the VRP in the vehicle reference frame.
                    // Around right/forward/up axes in radians per second.
                    // angular_velocity_vrf = new Ros.Point3D(),

                    // Roll/pitch/yaw that represents a rotation with intrinsic sequence z-x-y.
                    // in world coordinate (East/North/Up)
                    // The roll, in (-pi/2, pi/2), corresponds to a rotation around the y-axis.
                    // The pitch, in [-pi, pi), corresponds to a rotation around the x-axis.
                    // The yaw, in [-pi, pi), corresponds to a rotation around the z-axis.
                    // The direction of rotation follows the right-hand rule.
                    EulerAngles = new Apollo.Common.Point3D()
                    {
                        X = roll * Mathf.Deg2Rad,
                        Y = pitch * Mathf.Deg2Rad,
                        Z = yaw * Mathf.Deg2Rad,
                    }
                }
            };

            Apollo35WriterCorrectedImu.Publish(apolloIMUMessage);
        }

        else if (TargetRosEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1 || TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
        {
            var imu_msg = new Ros.Imu()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = Sequence++,
                    frame_id = ImuFrameId,
                },
                orientation = new Ros.Quaternion()
                {
                    x = orientation_unity.x,
                    y = orientation_unity.y,
                    z = orientation_unity.z,
                    w = orientation_unity.w,
                },
                orientation_covariance = new double[9],
                angular_velocity = new Ros.Vector3()
                {
                    x = angularVelocity.z,
                    y = - angularVelocity.x,
                    z = angularVelocity.y,
                },
                angular_velocity_covariance = new double[9],
                linear_acceleration = new Ros.Vector3()
                {
                    x = acceleration.z,
                    y = - acceleration.x,
                    z = acceleration.y,
                },
                linear_acceleration_covariance = new double[9],
            };
            AutowareWriterImu.Publish(imu_msg);

            var odom_msg = new Ros.Odometry()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = Sequence,
                    frame_id = OdometryFrameId,
                },
                child_frame_id = OdometryChildFrameId,
                pose = new Ros.PoseWithCovariance()
                {
                    pose = new Ros.Pose()
                    {
                        position = new Ros.Point()
                        {
                            x = odomPosition.x,
                            y = odomPosition.y,
                            z = odomPosition.z,
                        },
                        orientation = new Ros.Quaternion()
                        {
                            x = orientation_unity.x,
                            y = orientation_unity.y,
                            z = orientation_unity.z,
                            w = orientation_unity.w,
                        },
                    },
                    covariance = new double[36],
                },
                twist = new Ros.TwistWithCovariance()
                {
                    twist = new Ros.Twist()
                    {
                        linear = new Ros.Vector3(currVelocity.z, -currVelocity.x, currVelocity.y),
                        angular = new Ros.Vector3(angularVelocity.z, -angularVelocity.x, angularVelocity.y),
                    },
                    covariance = new double[36],
                },
            };
            AutowareWriterOdometry.Publish(odom_msg);

        }
        
    }
    private void AddUIElement()
    {
        if (Agent == null)
            Agent = transform.root.gameObject;
        var imuCheckbox = Agent.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleIMU", "Enable IMU:", isEnabled);
        imuCheckbox.onValueChanged.AddListener(x => Enable(x));
    }
}
