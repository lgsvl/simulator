/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using System.Collections.Generic;
using UnityEngine;

public class GpsDevice : MonoBehaviour, Comm.BridgeClient
{
    public Rigidbody mainRigidbody;
    public ROSTargetEnvironment targetEnv;

    public GameObject Target = null;
    public GameObject Agent = null;

    public string AutowareTopic = "/nmea_sentence";
    public string FrameId = "/gps";
    public string AutowareOdometryTopic = "/odom";

    public string ApolloTopic = "/apollo/sensor/gnss/best_pose";
    public string ApolloGPSOdometryTopic = "/apollo/sensor/gnss/odometry";
    public string ApolloInsStatTopic = "/apollo/sensor/gnss/ins_stat";

    public float Frequency = 12.5f;

    public float accuracy { get; private set; } = 0.01f; // just a number to report
    public double height { get; private set; } = 0; // sea level to WGS84 ellipsoid

    public double latitude { get; private set; }
    public double longitude { get; private set; }
    double easting;
    double northing;

    public double measurement_time { get; private set; }

    uint seq;

    float NextSend;

    public bool PublishMessage = false;

    Comm.Bridge Bridge;

    Comm.Writer<Ros.GnssBestPose> ApolloWriterGnssBestPose;
    Comm.Writer<Ros.Gps> ApolloWriterGps;
    Comm.Writer<apollo.drivers.gnss.GnssBestPose> Apollo35WriterGnssBestPose;
    Comm.Writer<apollo.localization.Gps> Apollo35WriterGps;
    Comm.Writer<apollo.drivers.gnss.InsStat> Apollo35WriterInsStat;
    Comm.Writer<Ros.Sentence> AutowareWriterSentence;
    Comm.Writer<Ros.Odometry> AutowareWriterOdometry;

    MapOrigin MapOrigin;

    private void Awake()
    {
        if (Agent == null)
            Agent = transform.root.gameObject;
        AddUIElement();

        MapOrigin = GameObject.Find("/MapOrigin").GetComponent<MapOrigin>();
    }

    private void Start()
    {
        NextSend = Time.time + 1.0f / Frequency;
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
            if (targetEnv == ROSTargetEnvironment.AUTOWARE)
            {
                AutowareWriterSentence = Bridge.AddWriter<Ros.Sentence>(AutowareTopic);
                AutowareWriterOdometry = Bridge.AddWriter<Ros.Odometry>(AutowareOdometryTopic);
            }

            else if (targetEnv == ROSTargetEnvironment.APOLLO)
            {
                ApolloWriterGnssBestPose = Bridge.AddWriter<Ros.GnssBestPose>(ApolloTopic);
                ApolloWriterGps = Bridge.AddWriter<Ros.Gps>(ApolloGPSOdometryTopic);
            }
            
            else if (targetEnv == ROSTargetEnvironment.APOLLO35)
            {
                Apollo35WriterGnssBestPose = Bridge.AddWriter<apollo.drivers.gnss.GnssBestPose>(ApolloTopic);
                Apollo35WriterGps = Bridge.AddWriter<apollo.localization.Gps>(ApolloGPSOdometryTopic);
                Apollo35WriterInsStat = Bridge.AddWriter<apollo.drivers.gnss.InsStat>(ApolloInsStatTopic);
            }
            seq = 0;
        };
    }

    void UpdateValues()
    {
        Vector3 pos = Target.transform.position;

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            // Autoware does not use origin/angle from map
            pos.x -= MapOrigin.OriginEasting - 500000;
            pos.z -= MapOrigin.OriginNorthing;
            pos = Quaternion.Euler(0f, -MapOrigin.Angle, 0f) * pos;
        }

        MapOrigin.GetNorthingEasting(pos, out northing, out easting);

        double lat, lon;
        MapOrigin.GetLatitudeLongitude(northing, easting, out lat, out lon);
        latitude = lat;
        longitude = lon;

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            // Autoware does not use UTMZoneId from map
            longitude -= (MapOrigin.UTMZoneId - 1) * 6 - 180 + 3;
        }
    }

    void Update()
    {
        if (targetEnv != ROSTargetEnvironment.APOLLO && targetEnv != ROSTargetEnvironment.APOLLO35 && targetEnv != ROSTargetEnvironment.AUTOWARE)
        {
            return;
        }

        if (Bridge == null || Bridge.Status != Comm.BridgeStatus.Connected || !PublishMessage)
        {
            return;
        }

        if (Time.time < NextSend)
        {
            return;
        }
        NextSend = Time.time + 1.0f / Frequency;

        UpdateValues();
        double altitude = transform.position.y + MapOrigin.AltitudeOffset; // above sea level

        var utc = System.DateTime.UtcNow.ToString("HHmmss.fff");

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            char latitudeS = latitude < 0.0f ? 'S' : 'N';
            char longitudeS = longitude < 0.0f ? 'W' : 'E';
            double lat = Math.Abs(latitude);
            double lon = Math.Abs(longitude);

            lat = Math.Floor(lat) * 100 + (lat % 1) * 60.0f;
            lon = Math.Floor(lon) * 100 + (lon % 1) * 60.0f;

            var gga = string.Format("GPGGA,{0},{1:0.000000},{2},{3:0.000000},{4},{5},{6},{7},{8:0.000000},M,{9:0.000000},M,,",
                utc,
                lat, latitudeS,
                lon, longitudeS,
                1, // GPX fix
                10, // sattelites tracked
                accuracy,
                altitude,
                height + MapOrigin.AltitudeOffset);

            var angles = Target.transform.eulerAngles;
            float roll = -angles.z;
            float pitch = -angles.x;
            float yaw = angles.y;

            var qq = string.Format("QQ02C,INSATT,V,{0},{1:0.000},{2:0.000},{3:0.000},",
                utc,
                roll,
                pitch,
                yaw);

            // http://www.plaisance-pratique.com/IMG/pdf/NMEA0183-2.pdf
            // 5.2.3 Checksum Field

            byte ggaChecksum = 0;
            for (int i = 0; i < gga.Length; i++)
            {
                ggaChecksum ^= (byte)gga[i];
            }

            byte qqChecksum = 0;
            for (int i = 0; i < qq.Length; i++)
            {
                qqChecksum ^= (byte)qq[i];
            }

            var ggaMessage = new Ros.Sentence()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seq++,
                    frame_id = FrameId,
                },
                sentence = "$" + gga + "*" + ggaChecksum.ToString("X2"),
            };
            AutowareWriterSentence.Publish(ggaMessage);

            var qqMessage = new Ros.Sentence()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seq++,
                    frame_id = FrameId,
                },
                sentence = qq + "@" + qqChecksum.ToString("X2"),

            };
            AutowareWriterSentence.Publish(qqMessage);

            // Autoware - GPS Odometry
            var quat = Quaternion.Euler(pitch, roll, yaw);
            float forward_speed = Vector3.Dot(mainRigidbody.velocity, Target.transform.forward);

            var odometryMessage = new Ros.Odometry()
            {
                header = new Ros.Header()
                {
                    stamp = Ros.Time.Now(),
                    seq = seq++,
                    frame_id = "odom",
                },
                child_frame_id = "base_link",
                pose = new Ros.PoseWithCovariance()
                {
                    pose = new Ros.Pose()
                    {
                        position = new Ros.Point()
                        {
                            x = easting + 500000,
                            y = northing,
                            z = 0.0,  // altitude,
                        },
                        orientation = new Ros.Quaternion()
                        {
                            x = quat.x,
                            y = quat.y,
                            z = quat.z,
                            w = quat.w,
                        }
                    }
                },
                twist = new Ros.TwistWithCovariance()
                {
                    twist = new Ros.Twist()
                    {
                        linear = new Ros.Vector3()
                        {
                            x = forward_speed,  // mainRigidbody.velocity.x,
                            y = 0.0,  // mainRigidbody.velocity.z,
                            z = 0.0,
                        },
                        angular = new Ros.Vector3()
                        {
                            x = 0.0,
                            y = 0.0,
                            z = -mainRigidbody.angularVelocity.y,
                        }
                    },
                }
            };
            AutowareWriterOdometry.Publish(odometryMessage);
        }
        else if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            // Apollo - GPS Best Pose
            System.DateTime GPSepoch = new System.DateTime(1980, 1, 6, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;
            var apolloMessage = new Ros.GnssBestPose()
            {
                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = seq++,
                },

                measurement_time = measurement_time,
                sol_status = 0,
                sol_type = 50,

                latitude = latitude,  // in degrees
                longitude = longitude,  // in degrees
                height_msl = height + MapOrigin.AltitudeOffset,  // height above mean sea level in meters
                undulation = 0,  // undulation = height_wgs84 - height_msl
                datum_id = 61,  // datum id number
                latitude_std_dev = accuracy,  // latitude standard deviation (m)
                longitude_std_dev = accuracy,  // longitude standard deviation (m)
                height_std_dev = accuracy,  // height standard deviation (m)
                base_station_id = "0",  // base station id
                differential_age = 2.0f,  // differential position age (sec)
                solution_age = 0.0f,  // solution age (sec)
                num_sats_tracked = 15,  // number of satellites tracked
                num_sats_in_solution = 15,  // number of satellites used in solution
                num_sats_l1 = 15,  // number of L1/E1/B1 satellites used in solution
                num_sats_multi = 12,  // number of multi-frequency satellites used in solution
                extended_solution_status = 33,  // extended solution status - OEMV and
                                                // greater only
                galileo_beidou_used_mask = 0,
                gps_glonass_used_mask = 51
            };
            ApolloWriterGnssBestPose.Publish(apolloMessage);

            // Apollo - GPS odometry
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;
            var angles = Target.transform.eulerAngles;
            float roll = angles.z;
            float pitch = angles.x;
            float yaw = -angles.y - MapOrigin.Angle;

            var quat = Quaternion.Euler(pitch, roll, yaw);
            Vector3 worldVelocity = mainRigidbody.velocity;

            var apolloGpsMessage = new Ros.Gps()
            {
                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = seq++,
                },

                localization = new Ros.ApolloPose()
                {
                    // Position of the vehicle reference point (VRP) in the map reference frame.
                    // The VRP is the center of rear axle.
                    position = new Ros.PointENU()
                    {
                        x = easting + 500000,  // East from the origin, in meters.
                        y = northing,  // North from the origin, in meters.
                        z = altitude  // Up from the WGS-84 ellipsoid, in
                                      // meters.
                    },

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the
                    // world coordinate (East/North/Up).
                    orientation = new Ros.ApolloQuaternion()
                    {
                        qx = quat.x,
                        qy = quat.y,
                        qz = quat.z,
                        qw = quat.w,
                    },

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    linear_velocity = new Ros.Point3D()
                    {
                        x = worldVelocity.x,
                        y = worldVelocity.z,
                        z = worldVelocity.y
                    },

                    // Linear acceleration of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    // linear_acceleration = new Ros.Point3D(),

                    // Angular velocity of the vehicle in the map reference frame.
                    // Around east/north/up axes in radians per second.
                    // angular_velocity = new Ros.Point3D(),

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
                    // euler_angles = new Ros.Point3D()
                }
            };
            ApolloWriterGps.Publish(apolloGpsMessage);
        }
        else if (targetEnv == ROSTargetEnvironment.APOLLO35)
        {
            // Apollo - GPS Best Pose
            System.DateTime GPSepoch = new System.DateTime(1980, 1, 6, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;
            var apolloMessage = new apollo.drivers.gnss.GnssBestPose()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = seq++,
                },

                measurement_time = measurement_time,
                sol_status = (apollo.drivers.gnss.SolutionStatus)0,
                sol_type = (apollo.drivers.gnss.SolutionType)50,

                latitude = latitude,  // in degrees
                longitude = longitude,  // in degrees
                height_msl = height + MapOrigin.AltitudeOffset,  // height above mean sea level in meters
                undulation = 0,  // undulation = height_wgs84 - height_msl
                datum_id = (apollo.drivers.gnss.DatumId)61,  // datum id number
                latitude_std_dev = accuracy,  // latitude standard deviation (m)
                longitude_std_dev = accuracy,  // longitude standard deviation (m)
                height_std_dev = accuracy,  // height standard deviation (m)
                base_station_id = System.Text.Encoding.UTF8.GetBytes("0"),  //CopyFrom((byte)"0"),  // base station id
                differential_age = 2.0f,  // differential position age (sec)
                solution_age = 0.0f,  // solution age (sec)
                num_sats_tracked = 15,  // number of satellites tracked
                num_sats_in_solution = 15,  // number of satellites used in solution
                num_sats_l1 = 15,  // number of L1/E1/B1 satellites used in solution
                num_sats_multi = 12,  // number of multi-frequency satellites used in solution
                extended_solution_status = 33,  // extended solution status - OEMV and
                                              // greater only
                galileo_beidou_used_mask = 0,
                gps_glonass_used_mask = 51
            };
            Apollo35WriterGnssBestPose.Publish(apolloMessage);

            // Apollo - GPS odometry
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;
            var angles = Target.transform.eulerAngles;
            float roll = angles.z;
            float pitch = angles.x;
            float yaw = -angles.y - MapOrigin.Angle;

            var quat = Quaternion.Euler(pitch, roll, yaw);
            Vector3 worldVelocity = mainRigidbody.velocity;

            var apolloGpsMessage = new apollo.localization.Gps()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = seq++,
                },

                localization = new apollo.localization.Pose()
                {
                    // Position of the vehicle reference point (VRP) in the map reference frame.
                    // The VRP is the center of rear axle.
                    position = new apollo.common.PointENU()
                    {
                        x = easting + 500000,  // East from the origin, in meters.
                        y = northing,  // North from the origin, in meters.
                        z = altitude  // Up from the WGS-84 ellipsoid, in
                                      // meters.
                    },

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the
                    // world coordinate (East/North/Up).
                    orientation = new apollo.common.Quaternion()
                    {
                        qx = quat.x,
                        qy = quat.y,
                        qz = quat.z,
                        qw = quat.w,
                    },

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    linear_acceleration = new apollo.common.Point3D()
                    {
                        x = worldVelocity.x,
                        y = worldVelocity.z,
                        z = worldVelocity.y
                    },

                    // Linear acceleration of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    // linear_acceleration = new Ros.Point3D(),

                    // Angular velocity of the vehicle in the map reference frame.
                    // Around east/north/up axes in radians per second.
                    // angular_velocity = new Ros.Point3D(),

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
                    // euler_angles = new Ros.Point3D()
                }
            };
            Apollo35WriterGps.Publish(apolloGpsMessage);

            var apolloInsMessage = new apollo.drivers.gnss.InsStat()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = 0,
                },

                ins_status = 3,
                pos_type = 56
            };
            Apollo35WriterInsStat.Publish(apolloInsMessage);
        }
    }

    public Api.Commands.GpsData GetData()
    {
        UpdateValues();

        var data = new Api.Commands.GpsData();
        data.Latitude = latitude;
        data.Longitude = longitude;
        data.Easting = easting + 500000;
        data.Northing = northing;
        data.Altitude = transform.position.y + MapOrigin.AltitudeOffset;
        data.Orientation = -transform.rotation.eulerAngles.y - MapOrigin.Angle;
        return data;
    }

    private void AddUIElement()
    {
        var gpsCheckbox = Agent.GetComponent<UserInterfaceTweakables>().AddCheckbox("ToggleGPS", "Enable GPS:", PublishMessage);
        gpsCheckbox.onValueChanged.AddListener(x => PublishMessage = x);
    }
}
