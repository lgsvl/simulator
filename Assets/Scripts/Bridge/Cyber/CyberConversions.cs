/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using UnityEngine;
using Simulator.Bridge.Data;
using System.Text;

namespace Simulator.Bridge.Cyber
{
    static class Conversions
    {
        static readonly DateTime GpsEpoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        static byte[] ActualBytes(byte[] array, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(array, 0, result, 0, length);
            return result;
        }

        public static apollo.drivers.CompressedImage ConvertFrom(ImageData data)
        {
            return new apollo.drivers.CompressedImage()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                    version = 1,
                    status = new apollo.common.StatusPb()
                    {
                        error_code = apollo.common.ErrorCode.OK,
                    },
                    frame_id = data.Frame,
                },
                measurement_time = data.Time,
                frame_id = data.Frame,
                format = "jpg",

                data = ActualBytes(data.Bytes, data.Length),
            };
        }

        public static apollo.drivers.PointCloud ConvertFrom(PointCloudData data)
        {
            var msg = new apollo.drivers.PointCloud()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                    version = 1,
                    status = new apollo.common.StatusPb()
                    {
                        error_code = apollo.common.ErrorCode.OK,
                    },
                    frame_id = data.Frame,
                },
                frame_id = data.Frame,
                is_dense = false,
                measurement_time = data.Time,
                height = 1,
                width = (uint)data.Points.Length, // TODO is this right?
            };

            for (int i = 0; i < data.Points.Length; i++)
            {
                var point = data.Points[i];
                if (point == Vector4.zero)
                {
                    continue;
                }

                var pos = new Vector3(point.x, point.y, point.z);
                float intensity = point.w;

                pos = data.Transform.MultiplyPoint3x4(pos);

                msg.point.Add(new apollo.drivers.PointXYZIT()
                {
                    x = pos.x,
                    y = pos.y,
                    z = pos.z,
                    intensity = (byte)(intensity * 255),
                    // timestamp = (ulong)0,
                });
            };

            return msg;
        }

        public static apollo.common.Detection2DArray ConvertFrom(Detected2DObjectData data)
        {
            var r = new apollo.common.Detection2DArray()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                },
            };

            foreach (var obj in data.Data)
            {
                r.detections.Add(new apollo.common.Detection2D()
                {
                    header = r.header,
                    id = obj.Id,
                    label = obj.Label,
                    score = obj.Score,
                    bbox = new apollo.common.BoundingBox2D()
                    {
                        x = obj.Position.x,
                        y = obj.Position.y,
                        width = obj.Scale.x,
                        height = obj.Scale.y
                    },
                    velocity = new apollo.common.Twist()
                    {
                        linear = ConvertToVector(obj.LinearVelocity),
                        angular = ConvertToVector(obj.LinearVelocity),
                    },
                });
            }

            return r;
        }

        public static apollo.common.Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            var r = new apollo.common.Detection3DArray()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                },
            };

            foreach (var obj in data.Data)
            {
                r.detections.Add(new apollo.common.Detection3D()
                {
                    header = r.header,
                    id = obj.Id,
                    label = obj.Label,
                    score = obj.Score,
                    bbox = new apollo.common.BoundingBox3D()
                    {
                        position = new apollo.common.Pose()
                        {
                            position = ConvertToPoint(obj.Position),
                            orientation = Convert(obj.Rotation),
                        },
                        size = ConvertToVector(obj.Scale),
                    },
                    velocity = new apollo.common.Twist()
                    {
                        linear = ConvertToVector(obj.LinearVelocity),
                        angular = ConvertToVector(obj.LinearVelocity),
                    },
                });
            }

            return r;
        }

        public static apollo.canbus.Chassis ConvertFrom(CanBusData data)
        {
            var eul = data.Orientation.eulerAngles;

            float dir;
            if (eul.y >= 0) dir = 45 * Mathf.Round((eul.y % 360) / 45.0f);
            else dir = 45 * Mathf.Round((eul.y % 360 + 360) / 45.0f);

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;
            var measurement_time = (dt - GpsEpoch).TotalSeconds + 18.0;
            var gpsTime = DateTimeOffset.FromUnixTimeSeconds((long)measurement_time).DateTime.ToLocalTime();

            return new apollo.canbus.Chassis()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = measurement_time,
                    module_name = "chassis",
                    sequence_num = data.Sequence,
                },

                engine_started = data.EngineOn,
                engine_rpm = data.EngineRPM,
                speed_mps = data.Speed,
                odometer_m = 0,
                fuel_range_m = 0,
                throttle_percentage = data.Throttle,
                brake_percentage = data.Breaking,
                steering_percentage = -data.Steering * 100,
                parking_brake = data.ParkingBrake,
                //high_beam_signal = data.HighBeamSignal,
                //low_beam_signal = data.LowBeamSignal,
                //left_turn_signal = data.LeftTurnSignal,
                //right_turn_signal = data.RightTurnSignal,
                wiper = data.Wipers,
                driving_mode = apollo.canbus.Chassis.DrivingMode.COMPLETE_AUTO_DRIVE,
                gear_location = data.InReverse ? apollo.canbus.Chassis.GearPosition.GEAR_REVERSE : apollo.canbus.Chassis.GearPosition.GEAR_DRIVE,

                chassis_gps = new apollo.canbus.ChassisGPS()
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
                    quality = apollo.canbus.GpsQuality.FIX_3D,
                    num_satellites = 15,
                    gps_speed = data.Velocity.magnitude,
                }
            };
        }

        public static apollo.drivers.gnss.GnssBestPose ConvertFrom(GpsData data)
        {
            float Accuracy = 0.01f; // just a number to report
            double Height = 0; // sea level to WGS84 ellipsoid

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;
            var measurement_time = (dt - GpsEpoch).TotalSeconds + 18.0;

            return new apollo.drivers.gnss.GnssBestPose()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = measurement_time,
                },
                measurement_time = measurement_time,
                sol_status = apollo.drivers.gnss.SolutionStatus.SOL_COMPUTED,
                sol_type = apollo.drivers.gnss.SolutionType.NARROW_INT,

                latitude = data.Latitude,  // in degrees
                longitude = data.Longitude,  // in degrees
                height_msl = Height,  // height above mean sea level in meters
                undulation = 0,  // undulation = height_wgs84 - height_msl
                datum_id = apollo.drivers.gnss.DatumId.WGS84,  // datum id number
                latitude_std_dev = Accuracy,  // latitude standard deviation (m)
                longitude_std_dev = Accuracy,  // longitude standard deviation (m)
                height_std_dev = Accuracy,  // height standard deviation (m)
                base_station_id = Encoding.UTF8.GetBytes("0"),  //CopyFrom((byte)"0"),  // base station id
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

        public static apollo.localization.Gps ConvertFrom(GpsOdometryData data)
        {
            var angles = data.Orientation.eulerAngles;
            float roll = angles.z;
            float pitch = angles.x;
            float yaw = -angles.y;
            var q = Quaternion.Euler(pitch, roll, yaw);

            return new apollo.localization.Gps()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                },

                localization = new apollo.localization.Pose()
                {
                    position = new apollo.common.PointENU()
                    {
                        x = data.Easting + 500000,  // East from the origin, in meters.
                        y = data.Northing,  // North from the origin, in meters.
                        z = data.Altitude// Up from the WGS-84 ellipsoid, in meters.
                    },

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the world coordinate (East/North/Up).
                    orientation = new apollo.common.Quaternion()
                    {
                        qx = q.x,
                        qy = q.y,
                        qz = q.z,
                        qw = q.w,
                    },

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    linear_acceleration = new apollo.common.Point3D()
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

        public static apollo.drivers.gnss.InsStat ConvertFrom(GpsInsData data)
        {
            return new apollo.drivers.gnss.InsStat()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                },

                ins_status = data.Status,
                pos_type = data.PositionType,
            };
        }

        public static Detected3DObjectArray ConvertTo(apollo.common.Detection3DArray data)
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
        
        public static VehicleControlData ConvertTo(apollo.control.ControlCommand data)
        {
            return new VehicleControlData()
            {
                Acceleration = (float)data.throttle / 100,
                Breaking = (float)data.brake / 100,
                SteerRate = (float)data.steering_rate,
                SteerTarget = (float)data.steering_target / 100,
                TimeStampSec = (float)data.header.timestamp_sec,
            };
        }

        public static apollo.drivers.gnss.Imu ConvertFrom(ImuData data)
        {
            return new apollo.drivers.gnss.Imu()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                },

                measurement_time = data.Time,
                measurement_span = (float)data.MeasurementSpan,
                linear_acceleration = ConvertToPoint(new Vector3(data.Acceleration.x, data.Acceleration.z, -data.Acceleration.y)),
                angular_velocity = ConvertToPoint(new Vector3(-data.AngularVelocity.z, data.AngularVelocity.x, -data.AngularVelocity.y)),
            };
        }

        public static apollo.localization.CorrectedImu ConvertFrom(CorrectedImuData data)
        {
            var angles = data.Orientation.eulerAngles;
            float roll = angles.x;
            float pitch = angles.y;
            float yaw = angles.z;

            return new apollo.localization.CorrectedImu()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                },

                imu = new apollo.localization.Pose()
                {
                    linear_acceleration = ConvertToPoint(new Vector3(data.Acceleration.x, data.Acceleration.z, -data.Acceleration.y)),
                    angular_velocity = ConvertToPoint(new Vector3(-data.AngularVelocity.z, data.AngularVelocity.x, -data.AngularVelocity.y)),
                    heading = yaw,
                    euler_angles = new apollo.common.Point3D()
                    {
                        x = roll * Mathf.Deg2Rad,
                        y = pitch * Mathf.Deg2Rad,
                        z = yaw * Mathf.Deg2Rad,
                    }
                }
            };
        }

        
        public static Detected2DObjectArray ConvertTo(apollo.common.Detection2DArray data)
        {
            return new Detected2DObjectArray()
            {
                Data = data.detections.Select(obj =>
                    new Detected2DObject()
                    {
                        Id = obj.id,
                        Label = obj.label,
                        Score = obj.score,
                        Position = new Vector2(obj.bbox.x, obj.bbox.y),
                        Scale = new Vector2(obj.bbox.width, obj.bbox.height),
                        LinearVelocity = Convert(obj.velocity.linear),
                        AngularVelocity = Convert(obj.velocity.angular),
                    }).ToArray(),
            };
        }

        static apollo.common.Point3D ConvertToPoint(Vector3 v)
        {
            return new apollo.common.Point3D() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Vector3 ConvertToVector(Vector3 v)
        {
            return new apollo.common.Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Quaternion Convert(Quaternion q)
        {
            return new apollo.common.Quaternion() { qx = q.x, qy = q.y, qz = q.z, qw = q.w };
        }

        static Vector3 Convert(apollo.common.Point3D p)
        {
            return new Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        static Vector3 Convert(apollo.common.Vector3 v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        static Quaternion Convert(apollo.common.Quaternion q)
        {
            return new Quaternion((float)q.qx, (float)q.qy, (float)q.qz, (float)q.qw);
        }
    }
}