/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Bridge.Data;
using Simulator.Bridge.Ros.LGSVL;

namespace Simulator.Bridge.Ros
{
    static class Conversions
    {
        static readonly DateTime GpsEpoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        public static CompressedImage ConvertFrom(ImageData data)
        {
            return new CompressedImage()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = ConvertTime(data.Time),
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
        
        public static LGSVL.Detection2DArray ConvertFrom(Detected2DObjectData data)
        {
            return new LGSVL.Detection2DArray()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                detections = data.Data.Select(d => new Detection2D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = d.Score,
                    bbox = new BoundingBox2D()
                    {
                        x = d.Position.x,
                        y = d.Position.y,
                        width = d.Scale.x,
                        height = d.Scale.y
                    },
                    velocity = new Twist()
                    {
                        linear = ConvertToVector(d.LinearVelocity),
                        angular = ConvertToVector(d.AngularVelocity),
                    }
                }).ToList(),
            };
        }

        public static Detected2DObjectArray ConvertTo(LGSVL.Detection2DArray data)
        {
            return new Detected2DObjectArray()
            {
                Data = data.detections.Select(obj =>
                    new Detected2DObject()
                    {
                        Id = obj.id,
                        Label = obj.label,
                        Score = obj.score,
                        Position = new UnityEngine.Vector2(obj.bbox.x, obj.bbox.y),
                        Scale = new UnityEngine.Vector2(obj.bbox.width, obj.bbox.height),
                        LinearVelocity = new UnityEngine.Vector3((float)obj.velocity.linear.x, 0, 0),
                        AngularVelocity = new UnityEngine.Vector3(0, 0, (float)obj.velocity.angular.z),
                    }).ToArray(),
            };
        }

        public static LGSVL.Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            return new LGSVL.Detection3DArray()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
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

        public static LGSVL.SignalArray ConvertFrom(SignalDataArray data)
        {
            return new LGSVL.SignalArray()
            {
                header = new Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                signals = data.Data.Select(d => new Signal()
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
                    }
                }).ToList(),
            };
        }

        public static Apollo.Drivers.ContiRadar ConvertFrom(DetectedRadarObjectData data)
        {
            var r = new Apollo.Drivers.ContiRadar()
            {
                header = new Apollo.Header()
                {
                    sequence_num = data.Sequence,
                    timestamp_sec = data.Time,
                    module_name = "conti_radar",
                },
                object_list_status = new Apollo.Drivers.ObjectListStatus_60A
                {
                    nof_objects = data.Data.Length,
                    meas_counter = 22800,
                    interface_version = 0
                },
                contiobs = new List<Apollo.Drivers.ContiRadarObs>(),
            };

            foreach (var obj in data.Data)
            {
                r.contiobs.Add(new Apollo.Drivers.ContiRadarObs()
                {
                    header = r.header,
                    clusterortrack = false,
                    obstacle_id = obj.Id,
                    longitude_dist = UnityEngine.Vector3.Project(obj.RelativePosition, obj.SensorAim).magnitude,
                    lateral_dist = UnityEngine.Vector3.Project(obj.RelativePosition, obj.SensorRight).magnitude * (UnityEngine.Vector3.Dot(obj.RelativePosition, obj.SensorRight) > 0 ? -1 : 1),
                    longitude_vel = UnityEngine.Vector3.Project(obj.RelativeVelocity, obj.SensorAim).magnitude * (UnityEngine.Vector3.Dot(obj.RelativeVelocity, obj.SensorAim) > 0 ? -1 : 1),
                    lateral_vel = UnityEngine.Vector3.Project(obj.RelativeVelocity, obj.SensorRight).magnitude * (UnityEngine.Vector3.Dot(obj.RelativeVelocity, obj.SensorRight) > 0 ? -1 : 1),
                    rcs = 11.0,
                    dynprop = obj.State, // 0 = moving, 1 = stationary, 2 = oncoming, 3 = stationary candidate, 4 = unknown, 5 = crossing stationary, 6 = crossing moving, 7 = stopped TODO use 2-7
                    longitude_dist_rms = 0,
                    lateral_dist_rms = 0,
                    longitude_vel_rms = 0,
                    lateral_vel_rms = 0,
                    probexist = 1.0, //prob confidence
                    meas_state = obj.NewDetection ? 1 : 2, //1 new 2 exist
                    longitude_accel = 0,
                    lateral_accel = 0,
                    oritation_angle = obj.SensorAngle,
                    longitude_accel_rms = 0,
                    lateral_accel_rms = 0,
                    oritation_angle_rms = 0,
                    length = obj.ColliderSize.z,
                    width = obj.ColliderSize.x,
                    obstacle_class = obj.ColliderSize.z > 5 ? 2 : 1, // 0: point; 1: car; 2: truck; 3: pedestrian; 4: motorcycle; 5: bicycle; 6: wide; 7: unknown // TODO set by type not size
                });
            }

            return r;
        }

        public static Apollo.ChassisMsg ConvertFrom(CanBusData data)
        {
            var eul = data.Orientation.eulerAngles;

            float dir;
            if (eul.y >= 0) dir = 45 * UnityEngine.Mathf.Round((eul.y % 360) / 45.0f);
            else dir = 45 * UnityEngine.Mathf.Round((eul.y % 360 + 360) / 45.0f);

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;
            var measurement_time = (dt - GpsEpoch).TotalSeconds + 18.0;
            var gpsTime = DateTimeOffset.FromUnixTimeSeconds((long)measurement_time).DateTime.ToLocalTime();

            return new Apollo.ChassisMsg()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = data.Time,
                    module_name = "chassis",
                    sequence_num = data.Sequence,
                },

                engine_started = data.EngineOn,
                engine_rpm = data.EngineRPM,
                speed_mps = data.Speed,
                odometer_m = 0,
                fuel_range_m = 0,
                throttle_percentage = data.Throttle,
                brake_percentage = data.Braking,
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

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;
            var measurement_time = (dt - GpsEpoch).TotalSeconds + 18.0;

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
                    stamp = ConvertTime(data.Time),
                    seq = data.Sequence, 
                    frame_id = data.Frame,
                },
                child_frame_id = "base_link",
                pose = new PoseWithCovariance()
                {
                    pose = new Pose()
                    {
                        position = new Point()
                        {
                            x = data.Easting + (data.IgnoreMapOrigin ? 0 : 500000),
                            y = data.Northing,
                            z = data.Altitude,
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

            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;

            return new Apollo.Gps()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = (dt - GpsEpoch).TotalSeconds + 18.0,
                    sequence_num = data.Sequence,
                },

                localization = new Apollo.Pose()
                {
                    // Position of the vehicle reference point (VRP) in the map reference frame.
                    // The VRP is the center of rear axle.
                    position = new Apollo.PointENU()
                    {
                        x = data.Easting,  // East from the origin, in meters.
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

        public static Detected3DObjectArray ConvertTo(LGSVL.Detection3DArray data)
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

        public static VehicleControlData ConvertTo(Autoware.VehicleCmd data)
        {
            var shiftUp = false;
            var shiftDown = false;

            if (data.gear != 0)
            {
                if (data.gear == 64)
                    shiftUp = true;
                else
                    shiftDown = true;
            }

            return new VehicleControlData()
            {
                Acceleration = (float)data.ctrl_cmd.linear_acceleration > 0 ? (float)data.ctrl_cmd.linear_acceleration : 0f,
                Breaking = (float)data.ctrl_cmd.linear_acceleration < 0 ? -(float)data.ctrl_cmd.linear_acceleration : 0f,
                Velocity = (float)data.twist_cmd.twist.linear.x,
                SteerAngularVelocity = (float)data.twist_cmd.twist.angular.z,
                SteerAngle = (float)data.ctrl_cmd.steering_angle,
                ShiftGearUp = shiftUp,
                ShiftGearDown = shiftDown,
            };
        }

        public static VehicleControlData ConvertTo(Apollo.control_command data)
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

        public static Imu ConvertFrom(ImuData data)
        {
            return new Imu()
            {
                header = new Header()
                {
                    stamp = ConvertTime(data.Time),
                    seq = data.Sequence,
                    frame_id = data.Frame,
                },

                orientation = Convert(data.Orientation),
                orientation_covariance = new double[9],
                angular_velocity = new Vector3() { x = data.AngularVelocity.z, y = -data.AngularVelocity.x, z = data.AngularVelocity.y },
                angular_velocity_covariance = new double[9],
                linear_acceleration = new Vector3() { x = data.Acceleration.z, y = -data.Acceleration.x, z = data.Acceleration.y },
                linear_acceleration_covariance = new double[9],
            };
        }

        public static Apollo.Imu ApolloConvertFrom(ImuData data)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(data.Time * 1000.0)).UtcDateTime;

            return new Apollo.Imu()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                },

                measurement_time = data.Time,
                measurement_span = (float)data.MeasurementSpan,
                linear_acceleration = new Apollo.Point3D() { x = data.Acceleration.x, y = data.Acceleration.z, z = -data.Acceleration.y },
                angular_velocity = new Apollo.Point3D() { x = -data.AngularVelocity.z, y = data.AngularVelocity.x, z = -data.AngularVelocity.y },
            };
        }

        public static Apollo.CorrectedImu ApolloConvertFrom(CorrectedImuData data)
        {
            var angles = data.Orientation.eulerAngles;
            float roll = angles.x;
            float pitch = angles.y;
            float yaw = angles.z;

            return new Apollo.CorrectedImu()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = data.Time,
                },

                imu = new Apollo.Pose()
                {
                    linear_acceleration = new Apollo.Point3D() { x = data.Acceleration.x, y = data.Acceleration.z, z = -data.Acceleration.y },
                    angular_velocity = new Apollo.Point3D() { x = -data.AngularVelocity.z, y = data.AngularVelocity.x, z = -data.AngularVelocity.y },
                    heading = yaw,
                    euler_angles = new Apollo.Point3D()
                    {
                        x = roll * UnityEngine.Mathf.Deg2Rad,
                        y = pitch * UnityEngine.Mathf.Deg2Rad,
                        z = yaw * UnityEngine.Mathf.Deg2Rad,
                    }
                }
            };
        }

        public static Clock ConvertFrom(ClockData data)
        {
            return new Clock()
            {
                clock = ConvertTime(data.Clock),
            };
        }

        public static VehicleControlData ConvertTo(TwistStamped data)
        {
            return new VehicleControlData()
            {
                SteerInput = (float)data.twist.angular.x,
            };
        }

        public static EmptySrv ConvertTo(Empty data)
        {
            return new EmptySrv();
        }

        public static Empty ConvertFrom(EmptySrv data)
        {
            return new Empty();
        }

        public static SetBoolSrv ConvertTo(SetBool data)
        {
            return new SetBoolSrv()
            {
                data = data.data,
            };
        }

        public static SetBoolResponse ConvertFrom(SetBoolSrv data)
        {
            return new SetBoolResponse()
            {
                success = data.data,
                message = data.message,
            };
        }

        public static Trigger ConvertFrom(TriggerSrv data)
        {
            return new Trigger()
            {
                success = data.data,
                message = data.message,
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

        public static Time ConvertTime(double unixEpochSeconds)
        {
            long nanosec = (long)(unixEpochSeconds * 1e9);

            return new Time()
            {
                secs = nanosec / 1000000000,
                nsecs = (uint)(nanosec % 1000000000),
            };
        }
    }
}
