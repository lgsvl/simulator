/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros.LGSVL;

namespace Simulator.Bridge.Ros
{
    static class Conversions
    {
        public static CompressedImage ConvertFrom(ImageData data)
        {
            return new CompressedImage()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(), // TODO: time should be virtual Unity time, not real world time
                    frame_id = data.Frame,
                },
                format = "jpeg",
                data = new PartialByteArray()
                {
                    Array = data.Bytes,
                    Length = data.Length,
                },
            };
        }

        public static Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            return new Detection3DArray()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Time.Now(), // TODO
                    frame_id = data.Frame,
                },
                detections = data.Data.Select(d => new Detection3D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = d.Score,
                    bbox = new BoundingBox3D()
                    {
                        position = new Pose()
                        {
                            position = ConvertToPoint(d.Position),
                            orientation = Convert(d.Rotation),
                        },
                        size = ConvertToVector(d.Scale),
                    },
                    velocity = new Twist()
                    {
                        linear = ConvertToVector(d.LinearVelocity),
                        angular = ConvertToVector(d.AngularVelocity),
                    }
                }).ToList(),
            };
        }

        public static Apollo.ChassisMsg ConvertFrom(CanBusData data)
        {
            var eul = data.Orientation.eulerAngles;

            float dir;
            if (eul.y >= 0) dir = 45 * UnityEngine.Mathf.Round((eul.y % 360) / 45.0f);
            else dir = 45 * UnityEngine.Mathf.Round((eul.y % 360 + 360) / 45.0f);

            // TODO
            DateTime GPSepoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            var measurement_time = (System.DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0;
            var gpsTime = DateTimeOffset.FromUnixTimeSeconds((long)measurement_time).DateTime.ToLocalTime();

            return new Apollo.ChassisMsg()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = Time.Now().secs, // TODO,
                    module_name = "chassis",
                    sequence_num = data.Sequence,
                },

                engine_started = data.EngineOn,
                engine_rpm = data.EngineRPM,
                speed_mps = MetersPerSecondToMilesPerHour(data.Speed),
                odometer_m = 0,
                fuel_range_m = 0,
                throttle_percentage = data.Throttle,
                brake_percentage = data.Breaking,
                steering_percentage = -data.Steering * 100,
                parking_brake = data.ParkingBrake,
                high_beam_signal = data.HighBeamSignal,
                low_beam_signal = data.LowBeamSignal,
                left_turn_signal = data.LeftTurnSignal,
                right_turn_signal = data.RightTurnSignal,
                wiper = data.Wipers,
                driving_mode = Apollo.Chassis.DrivingMode.COMPLETE_AUTO_DRIVE,
                gear_location = data.InReverse ? Apollo.Chassis.GearPosition.GEAR_REVERSE : Apollo.Chassis.GearPosition.GEAR_DRIVE,

                chassis_gps = new Apollo.Chassis.ChassisGPS()
                {
                    latitude = data.Latitude,
                    longitude = data.Longitude,
                    gps_valid = true,
                    year = gpsTime.Year,
                    month = gpsTime.Month,
                    day = gpsTime.Day,
                    hours = gpsTime.Hour,
                    minutes = gpsTime.Minute,
                    seconds = gpsTime.Second,
                    compass_direction = dir,
                    pdop = 0.1,
                    is_gps_fault = false,
                    is_inferred = false,
                    altitude = data.Altitude,
                    heading = eul.y,
                    hdop = 0.1,
                    vdop = 0.1,
                    quality = Apollo.Chassis.GpsQuality.FIX_3D,
                    num_satellites = 15,
                    gps_speed = data.Velocity.magnitude,
                }
            };
        }

        public static Apollo.GnssBestPose ConvertFrom(GpsData data)
        {
            float Accuracy = 0.01f; // just a number to report
            double Height = 0; // sea level to WGS84 ellipsoid

            // TODO: time
            DateTime GPSepoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            double measurement_time = (DateTime.UtcNow - GPSepoch).TotalSeconds + 18.0f;

            return new Apollo.GnssBestPose()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = measurement_time,
                    sequence_num = data.Sequence++,
                },

                measurement_time = measurement_time,
                sol_status = 0,
                sol_type = 50,

                latitude = data.Latitude,
                longitude = data.Longitude,
                height_msl = Height,
                undulation = 0,
                datum_id = 61,  // datum id number
                latitude_std_dev = Accuracy,  // latitude standard deviation (m)
                longitude_std_dev = Accuracy,  // longitude standard deviation (m)
                height_std_dev = Accuracy,  // height standard deviation (m)
                base_station_id = "0",  // base station id
                differential_age = 2.0f,  // differential position age (sec)
                solution_age = 0.0f,  // solution age (sec)
                num_sats_tracked = 15,  // number of satellites tracked
                num_sats_in_solution = 15,  // number of satellites used in solution
                num_sats_l1 = 15,  // number of L1/E1/B1 satellites used in solution
                num_sats_multi = 12,  // number of multi-frequency satellites used in solution
                extended_solution_status = 33,  // extended solution status - OEMV and greater only
                galileo_beidou_used_mask = 0,
                gps_glonass_used_mask = 51
            };
        }

        public static Odometry ConvertFrom(GpsOdometryData data)
        {
            return new Odometry()
            {
                header = new Header()
                {
                    stamp = Time.Now(), // tODO
                    seq = data.Sequence, 
                    frame_id = data.Frame,
                },
                child_frame_id = "base_link",
                pose = new PoseWithCovariance()
                {
                    pose = new Pose()
                    {
                        position = new Ros.Point()
                        {
                            x = data.Easting + 500000,
                            y = data.Northing,
                            z = 0.0,
                        },
                        orientation = Convert(data.Orientation),
                    }
                },
                twist = new TwistWithCovariance()
                {
                    twist = new Twist()
                    {
                        linear = new Vector3()
                        {
                            x = data.ForwardSpeed,
                            y = 0.0,
                            z = 0.0,
                        },
                        angular = new Vector3()
                        {
                            x = 0.0,
                            y = 0.0,
                            z = - data.AngularVelocity.y,
                        }
                    },
                }
            };
        }

        public static Apollo.Gps ApolloConvertFrom(GpsOdometryData data)
        {
            var angles = data.Orientation.eulerAngles;
            float roll = angles.z;
            float pitch = angles.x;
            float yaw = -angles.y;
            var q = UnityEngine.Quaternion.Euler(pitch, roll, yaw);

            return new Apollo.Gps()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = 0, // TODO
                    sequence_num = data.Sequence++,
                },

                localization = new Apollo.Pose()
                {
                    // Position of the vehicle reference point (VRP) in the map reference frame.
                    // The VRP is the center of rear axle.
                    position = new Apollo.PointENU()
                    {
                        x = data.Easting + 500000,  // East from the origin, in meters.
                        y = data.Northing,  // North from the origin, in meters.
                        z = data.Altitude  // Up from the WGS-84 ellipsoid, in meters.
                    },

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the world coordinate (East/North/Up).
                    orientation = new Apollo.Quaternion()
                    {
                        qx = q.x,
                        qy = q.y,
                        qz = q.z,
                        qw = q.w,
                    },

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    linear_velocity = new Apollo.Point3D()
                    {
                        x = data.Velocity.x,
                        y = data.Velocity.z,
                        z = data.Velocity.y,
                    },

                    // The heading is zero when the car is facing East and positive when facing North.
                    heading = yaw,  // not used ??
                }
            };
        }

        public static Detected3DObjectArray ConvertTo(Detection3DArray data)
        {
            return new Detected3DObjectArray()
            {
                Data = data.detections.Select(obj =>
                    new Detected3DObject()
                    {
                        Id = obj.id,
                        Label = obj.label,
                        Score = obj.score,
                        Position = Convert(obj.bbox.position.position),
                        Rotation = Convert(obj.bbox.position.orientation),
                        Scale = Convert(obj.bbox.size),
                        LinearVelocity = Convert(obj.velocity.linear),
                        AngularVelocity = Convert(obj.velocity.angular),
                    }).ToArray(),
            };
        }

        static Point ConvertToPoint(UnityEngine.Vector3 v)
        {
            return new Point() { x = v.x, y = v.y, z = v.z };
        }

        static Vector3 ConvertToVector(UnityEngine.Vector3 v)
        {
            return new Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static Quaternion Convert(UnityEngine.Quaternion q)
        {
            return new Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
        }

        static UnityEngine.Vector3 Convert(Point p)
        {
            return new UnityEngine.Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        static UnityEngine.Vector3 Convert(Vector3 v)
        {
            return new UnityEngine.Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        static UnityEngine.Quaternion Convert(Quaternion q)
        {
            return new UnityEngine.Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
        }

        public static float MetersPerSecondToMilesPerHour(float speed)
        {
            return speed * 2.23693629f;
    }
    }
}
