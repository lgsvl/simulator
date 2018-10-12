/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System;
using UnityEngine;

public class GpsDevice : MonoBehaviour, Ros.IRosClient
{
    public Rigidbody mainRigidbody;
    public ROSTargetEnvironment targetEnv;

    public double OriginNorthing = 4182486.0f;
    public double OriginEasting = 552874.0f;
    public int UTMZoneId = 10;

    public GameObject Target = null;

    public string AutowareTopic = "/nmea_sentence";
    public string FrameId = "/gps";

    public string ApolloTopic = "/apollo/sensor/gnss/best_pose";
    public string ApolloGPSOdometryTopic = "/apollo/sensor/gnss/odometry";

    public float Scale = 1.0f;
    public float Angle = -45.3f;

    public float Frequency = 12.5f;

    public float accuracy { get; private set; }
    public double height { get; private set; }
    public double latitude_orig { get; private set; }
    public double longitude_orig { get; private set; }

    public double measurement_time { get; private set; }
    
    uint seq;

    float NextSend;

    public bool PublishMessage = false;

    Ros.Bridge Bridge;

    private void Start()
    {
        NextSend = Time.time + 1.0f / Frequency;
    }

    public void OnRosBridgeAvailable(Ros.Bridge bridge)
    {
        Bridge = bridge;
        Bridge.AddPublisher(this);
    }

    public void OnRosConnected()
    {
        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            Bridge.AddPublisher<Ros.Sentence>(AutowareTopic);
        }

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            Bridge.AddPublisher<Ros.GnssBestPose>(ApolloTopic);
            Bridge.AddPublisher<Ros.Gps>(ApolloGPSOdometryTopic);
        }

        seq = 0;
    }

    void Update()
    {
        if (targetEnv != ROSTargetEnvironment.APOLLO && targetEnv != ROSTargetEnvironment.AUTOWARE)
        {
            return;
        }

        if (Bridge == null || Bridge.Status != Ros.Status.Connected || !PublishMessage)
        {
            return;
        }

        if (Time.time < NextSend)
        {
            return;
        }
        NextSend = Time.time + 1.0f / Frequency;

        Vector3 pos = Target.transform.position;

        // rotate - 45 deg
        pos = Quaternion.Euler(0f, Angle, 0f) * pos;
        var utc = System.DateTime.UtcNow.ToString("HHmmss.fff");

        accuracy = 0.01f; // just a number to report
        double altitude = pos.y; // above sea level
        height = 0; // sea level to WGS84 ellipsoid

        double easting = pos.x * Scale;
        double northing = pos.z * Scale;

        if (targetEnv == ROSTargetEnvironment.APOLLO) {
            easting = easting + OriginEasting;
            northing = northing + OriginNorthing;
            easting = easting - 500000;
        }

        // MIT licensed conversion code from https://github.com/Turbo87/utm/blob/master/utm/conversion.py

        double K0 = 0.9996;

        double E = 0.00669438;
        double E2 = E * E;
        double E3 = E2 * E;
        double E_P2 = E / (1.0 - E);

        double SQRT_E = Math.Sqrt(1 - E);
        double _E = (1 - SQRT_E) / (1 + SQRT_E);
        double _E2 = _E * _E;
        double _E3 = _E2 * _E;
        double _E4 = _E3 * _E;
        double _E5 = _E4 * _E;

        double M1 = (1 - E / 4 - 3 * E2 / 64 - 5 * E3 / 256);

        double P2 = (3.0 / 2 * _E - 27.0 / 32 * _E3 + 269.0 / 512 * _E5);
        double P3 = (21.0 / 16 * _E2 - 55.0 / 32 * _E4);
        double P4 = (151.0 / 96 * _E3 - 417.0 / 128 * _E5);
        double P5 = (1097.0 / 512 * _E4);

        double R = 6378137;

        double x = easting;
        double y = northing;

        double m = y / K0;
        double mu = m / (R * M1);

        double p_rad = (mu +
                 P2 * Math.Sin(2 * mu) +
                 P3 * Math.Sin(4 * mu) +
                 P4 * Math.Sin(6 * mu) +
                 P5 * Math.Sin(8 * mu));

        double p_sin = Math.Sin(p_rad);
        double p_sin2 = p_sin * p_sin;

        double p_cos = Math.Cos(p_rad);

        double p_tan = p_sin / p_cos;
        double p_tan2 = p_tan * p_tan;
        double p_tan4 = p_tan2 * p_tan2;

        double ep_sin = 1 - E * p_sin2;
        double ep_sin_sqrt = Math.Sqrt(1 - E * p_sin2);

        double n = R / ep_sin_sqrt;
        double r = (1 - E) / ep_sin;

        double c = _E * p_cos * p_cos;
        double c2 = c * c;

        double d = x / (n * K0);
        double d2 = d * d;
        double d3 = d2 * d;
        double d4 = d3 * d;
        double d5 = d4 * d;
        double d6 = d5 * d;

        double lat = (p_rad - (p_tan / r) *
                    (d2 / 2 -
                     d4 / 24 * (5 + 3 * p_tan2 + 10 * c - 4 * c2 - 9 * E_P2)) +
                     d6 / 720 * (61 + 90 * p_tan2 + 298 * c + 45 * p_tan4 - 252 * E_P2 - 3 * c2));

        double lon = (d -
                     d3 / 6 * (1 + 2 * p_tan2 + c) +
                     d5 / 120 * (5 - 2 * c + 28 * p_tan2 - 3 * c2 + 8 * E_P2 + 24 * p_tan4)) / p_cos;

        latitude_orig = (double)(lat * 180.0 / Math.PI);
        longitude_orig = (double)(lon * 180.0 / Math.PI);
        if (targetEnv == ROSTargetEnvironment.APOLLO && UTMZoneId > 0) {
            longitude_orig = longitude_orig + (UTMZoneId - 1) * 6 - 180 + 3;
        }

        //

        if (targetEnv == ROSTargetEnvironment.AUTOWARE)
        {
            char latitudeS = latitude_orig < 0.0f ? 'S' : 'N';
            char longitudeS = longitude_orig < 0.0f ? 'W' : 'E';
            double latitude = Math.Abs(latitude_orig);
            double longitude = Math.Abs(longitude_orig);

            latitude = Math.Floor(latitude) * 100 + (latitude % 1) * 60.0f;
            longitude = Math.Floor(longitude) * 100 + (longitude % 1) * 60.0f;

            var gga = string.Format("GPGGA,{0},{1:0.000000},{2},{3:0.000000},{4},{5},{6},{7},{8:0.000000},M,{9:0.000000},M,,",
                utc,
                latitude, latitudeS,
                longitude, longitudeS,
                1, // GPX fix
                10, // sattelites tracked
                accuracy,
                altitude,
                height);

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
            Bridge.Publish(AutowareTopic, ggaMessage);

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
            Bridge.Publish(AutowareTopic, qqMessage);
        }        

        if (targetEnv == ROSTargetEnvironment.APOLLO)
        {
            // Apollo - GPS Best Pose
            System.DateTime GPSepoch = new System.DateTime(1980, 1, 6, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;
            var apolloMessage = new Ros.GnssBestPose()
            {
                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = (int)seq++,
                },

                measurement_time = measurement_time,
                sol_status = 0,
                sol_type = 50,

                latitude = latitude_orig,  // in degrees
                longitude = longitude_orig,  // in degrees
                height_msl = height,  // height above mean sea level in meters
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
            Bridge.Publish(ApolloTopic, apolloMessage);

            // Apollo - GPS odometry
            System.DateTime Unixepoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            measurement_time = (double)(System.DateTime.UtcNow - Unixepoch).TotalSeconds;
            var angles = Target.transform.eulerAngles;
            float roll = angles.z;
            float pitch = angles.x;
            float yaw = -angles.y - Angle;

            var quat = Quaternion.Euler(pitch, roll, yaw);
            Vector3 worldVelocity = mainRigidbody.velocity;

            var apolloGpsMessage = new Ros.Gps()
            {
                header = new Ros.ApolloHeader()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = (int)seq++,
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
            Bridge.Publish(ApolloGPSOdometryTopic, apolloGpsMessage);
        }        
    }
}
