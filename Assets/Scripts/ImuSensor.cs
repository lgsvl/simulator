/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


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
    Comm.Writer<apollo.drivers.gnss.Imu> Apollo35WriterImu;
    Comm.Writer<apollo.localization.CorrectedImu> Apollo35WriterCorrectedImu;

    Comm.Writer<Ros.Imu> AutowareWriterImu;
    Comm.Writer<Ros.Odometry> AutowareWriterOdometry;
    public Rigidbody mainRigidbody;
    public GameObject Target;
    private GameObject Agent;
    public bool PublishMessage = false;

    [HideInInspector]
    public bool IsEnabled;
    uint Sequence;

    public float Frequency = 100.0f;
    Queue<Tuple<TimeSpan, Action>> MessageQueue;
    bool IsImuDestroyed = false;
    bool IsFirstFixedUpdate = true;
    TimeSpan CurrTimestamp;
    TimeSpan LastTimestamp;
    TimeSpan Interval;

    private void Awake()
    {
        AddUIElement();
        IsImuDestroyed = false;
    }

    private void Start()
    {
        lastVelocity = Vector3.zero;

        MessageQueue = new Queue<Tuple<TimeSpan, Action>>();
        Task.Run(() => Publish());
        Interval = TimeSpan.FromMilliseconds((double)(1.0f / Frequency * 1000.0f));  // 100 hz = 10 ms
    }

    public void Publish()
    {
        long nextPublish = Stopwatch.GetTimestamp();
        while (IsImuDestroyed == false)
        {
            long now = Stopwatch.GetTimestamp();
            if (now < nextPublish)
            {
                Thread.Sleep(0);
                continue;
            }

            Tuple<TimeSpan, Action> msg = null;
            lock (MessageQueue)
            {
                if (MessageQueue.Count > 0)
                {
                    msg = MessageQueue.Dequeue();
                }
            }

            if (msg != null)
            {
                try
                {
                    Action action = msg.Item2;
                    action();
                    nextPublish = now + (long)(Stopwatch.Frequency / Frequency);
                    LastTimestamp = msg.Item1;
                }
                catch
                {
                    // Do nothing;
                }
            }
        }

        MessageQueue.Clear();
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
                Apollo35WriterImu = Bridge.AddWriter<apollo.drivers.gnss.Imu>(ImuTopic);
                Apollo35WriterCorrectedImu = Bridge.AddWriter<apollo.localization.CorrectedImu>(ApolloIMUOdometryTopic);
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

    public void Update()
    {
        IsFirstFixedUpdate = true;
    }

    public void FixedUpdate()
    {
        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected || !PublishMessage || !IsEnabled)
        {
            return;
        }

        System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        if (IsFirstFixedUpdate)
        {
            lock (MessageQueue)
            {
                MessageQueue.Clear();
            }

            CurrTimestamp = System.DateTime.UtcNow - Unixepoch;
            IsFirstFixedUpdate = false;
        }
        else
        {
            CurrTimestamp = CurrTimestamp.Add(Interval);
        }

        if (TimeSpan.Compare(CurrTimestamp, LastTimestamp) == -1)  // if CurrTimestamp < LastTimestamp
        {
            return;
        }

        Vector3 currVelocity = transform.InverseTransformDirection(mainRigidbody.velocity);
        Vector3 acceleration = (currVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currVelocity;

        Vector3 angularVelocity = mainRigidbody.angularVelocity;

        var angles = Target.transform.eulerAngles;
        float roll = -angles.z;
        float pitch = -angles.x;
        float yaw = -angles.y;
        Quaternion orientation_unity = Quaternion.Euler(roll, pitch, yaw);
        
        double measurement_time = (double)CurrTimestamp.TotalSeconds;
        float measurement_span = (float)(1 / Frequency);

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
                z = -acceleration.y,
            };
            var angular_velocity = new Ros.Point3D()
            {
                x = -angularVelocity.z,
                y = angularVelocity.x,
                z = -angularVelocity.y,
            };

            var apolloImuMsg = new Ros.Apollo.Imu()
            {
                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = measurement_time
                },
                measurement_time = measurement_time,
                measurement_span = measurement_span,
                linear_acceleration = linear_acceleration,
                angular_velocity = angular_velocity
            };
            
            var apolloCorrectedImuMsg = new Ros.CorrectedImu()
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

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(new Tuple<TimeSpan, Action>(CurrTimestamp, () => {
                    ApolloWriterImu.Publish(apolloImuMsg);
                    ApolloWriterCorrectedImu.Publish(apolloCorrectedImuMsg);
                }));
            }
        }
        else if (TargetRosEnv == ROSTargetEnvironment.APOLLO35)
        {
            var linear_acceleration = new apollo.common.Point3D()
            {
                x = acceleration.x,
                y = acceleration.z,
                z = -acceleration.y,
            };
            var angular_velocity = new apollo.common.Point3D()
            {
                x = -angularVelocity.z,
                y = angularVelocity.x,
                z = -angularVelocity.y,
            };

            var apollo35ImuMsg = new apollo.drivers.gnss.Imu()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time
                },
                measurement_time = measurement_time,
                measurement_span = measurement_span,
                linear_acceleration = linear_acceleration,
                angular_velocity = angular_velocity
            };

            var apollo35CorrectedImuMsg = new apollo.localization.CorrectedImu()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time                  
                },
                imu = new apollo.localization.Pose()
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
                    euler_angles = new apollo.common.Point3D()
                    {
                        x = roll * Mathf.Deg2Rad,
                        y = pitch * Mathf.Deg2Rad,
                        z = yaw * Mathf.Deg2Rad,
                    }
                }
            };

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(new Tuple<TimeSpan, Action>(CurrTimestamp, () => {
                    Apollo35WriterImu.Publish(apollo35ImuMsg);
                    Apollo35WriterCorrectedImu.Publish(apollo35CorrectedImuMsg);
                }));
            }
        }
        else if (TargetRosEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1 || TargetRosEnv == ROSTargetEnvironment.AUTOWARE)
        {
            var nanoSec = 1000000 * CurrTimestamp.TotalMilliseconds;
            var autowareImuMsg = new Ros.Imu()
            {
                header = new Ros.Header()
                {
                    stamp = new Ros.Time()
                    {
                        secs = (long)nanoSec / 1000000000,
                        nsecs = (uint)nanoSec % 1000000000,
                    },
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

            var autowareOdomMsg = new Ros.Odometry()
            {
                header = new Ros.Header()
                {
                    stamp = new Ros.Time()
                    {
                        secs = (long)nanoSec / 1000000000,
                        nsecs = (uint)nanoSec % 1000000000,
                    },
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

            lock (MessageQueue)
            {
                MessageQueue.Enqueue(new Tuple<TimeSpan, Action>(CurrTimestamp, () => {
                    AutowareWriterImu.Publish(autowareImuMsg);
                    if (TargetRosEnv == ROSTargetEnvironment.DUCKIETOWN_ROS1)
                    {
                        AutowareWriterOdometry.Publish(autowareOdomMsg);
                    }
                }));
            }
        }
    }

    private void AddUIElement()
    {
        if (Agent == null)
            Agent = transform.root.gameObject;
        var imuCheckbox = Agent.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleIMU", "Enable IMU:", IsEnabled);
        imuCheckbox.onValueChanged.AddListener(x => IsEnabled = x);
    }

    private void OnDestroy()
    {
        IsImuDestroyed = true;
    }
}
