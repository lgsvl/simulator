/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Bridge.Data;
using Unity.Mathematics;
// NOTE: DO NOT add using "Ros2.Ros" or "Ros2.Lgsvl" namespaces here to avoid
// NOTE: confusion between types. Keep them fully qualified in this file.

namespace Simulator.Bridge.Ros2
{
    static class Ros2Conversions
    {
        public static Ros.CompressedImage ConvertFrom(ImageData data)
        {
            return new Ros.CompressedImage()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
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

        public static Lgsvl.Detection2DArray ConvertFrom(Detected2DObjectData data)
        {
            return new Lgsvl.Detection2DArray()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                detections = data.Data.Select(d => new Lgsvl.Detection2D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = (float)d.Score,
                    bbox = new Lgsvl.BoundingBox2D()
                    {
                        x = d.Position.x,
                        y = d.Position.y,
                        width = d.Scale.x,
                        height = d.Scale.y
                    },
                    velocity = new Ros.Twist()
                    {
                        linear = ConvertToVector(d.LinearVelocity),
                        angular = ConvertToVector(d.AngularVelocity),
                    }
                }).ToList(),
            };
        }

        public static Detected2DObjectArray ConvertTo(Lgsvl.Detection2DArray data)
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

        public static Lgsvl.Detection3DArray ConvertFrom(Detected3DObjectData data)
        {
            var arr = new Lgsvl.Detection3DArray()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                detections = new List<Lgsvl.Detection3D>(),
            };

            foreach (var d in data.Data)
            {
                // Transform from (Right/Up/Forward) to (Forward/Left/Up)
                var position = d.Position;
                position.Set(position.z, -position.x, position.y);

                var orientation = d.Rotation;
                orientation.Set(-orientation.z, orientation.x, -orientation.y, orientation.w);

                var size = d.Scale;
                size.Set(size.z, size.x, size.y);

                d.AngularVelocity.z = -d.AngularVelocity.z;

                var det = new Lgsvl.Detection3D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = (float)d.Score,
                    bbox = new Lgsvl.BoundingBox3D()
                    {
                        position = new Ros.Pose()
                        {
                            position = ConvertToPoint(position),
                            orientation = Convert(orientation),
                        },
                        size = ConvertToVector(size),
                    },
                    velocity = new Ros.Twist()
                    {
                        linear = ConvertToVector(d.LinearVelocity),
                        angular = ConvertToVector(d.AngularVelocity),
                    },
                };

                arr.detections.Add(det);
            }

            return arr;
        }

        public static Lgsvl.SignalArray ConvertFrom(SignalDataArray data)
        {
            return new Lgsvl.SignalArray()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                signals = data.Data.Select(d => new Lgsvl.Signal()
                {
                    id = d.SeqId,
                    label = d.Label,
                    score = (float)d.Score,
                    bbox = new Lgsvl.BoundingBox3D()
                    {
                        position = new Ros.Pose()
                        {
                            position = ConvertToPoint(d.Position),
                            orientation = Convert(d.Rotation),
                        },
                        size = ConvertToVector(d.Scale),
                    }
                }).ToList(),
            };
        }

        public static Lgsvl.CanBusData ConvertFrom(CanBusData data)
        {
            return new Lgsvl.CanBusData()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                speed_mps = data.Speed,
                throttle_pct = data.Throttle,
                brake_pct = data.Braking,
                steer_pct = data.Steering,
                parking_brake_active = false,   // parking brake is not supported in Simulator side
                high_beams_active = data.HighBeamSignal,
                low_beams_active = data.LowBeamSignal,
                hazard_lights_active = data.HazardLights,
                fog_lights_active = data.FogLights,
                left_turn_signal_active = data.LeftTurnSignal,
                right_turn_signal_active = data.RightTurnSignal,
                wipers_active = data.Wipers,
                reverse_gear_active = data.InReverse,
                selected_gear = (data.InReverse ? Lgsvl.Gear.GEAR_REVERSE : Lgsvl.Gear.GEAR_DRIVE),
                engine_active = data.EngineOn,
                engine_rpm = data.EngineRPM,
                gps_latitude = data.Latitude,
                gps_longitude = data.Longitude,
                gps_altitude = data.Altitude,
                orientation = Convert(data.Orientation),
                linear_velocities = ConvertToVector(data.Velocity),
            };
        }

        public static Lgsvl.DetectedRadarObjectArray ConvertFrom(DetectedRadarObjectData data)
        {
            var r = new Lgsvl.DetectedRadarObjectArray()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
            };

            foreach (var obj in data.Data)
            {
                r.objects.Add(new Lgsvl.DetectedRadarObject()
                {
                    sensor_aim = ConvertToVector(obj.SensorAim),
                    sensor_right = ConvertToVector(obj.SensorRight),
                    sensor_position = ConvertToPoint(obj.SensorPosition),
                    sensor_velocity = ConvertToVector(obj.SensorVelocity),
                    sensor_angle = obj.SensorAngle,
                    object_position = ConvertToPoint(obj.Position),
                    object_velocity = ConvertToVector(obj.Velocity),
                    object_relative_position = ConvertToPoint(obj.RelativePosition),
                    object_relative_velocity = ConvertToVector(obj.RelativeVelocity),
                    object_collider_size = ConvertToVector(obj.ColliderSize),
                    object_state = (byte)obj.State,
                    new_detection = obj.NewDetection,
                });
            }

            return r;
        }

        public static Lgsvl.Ultrasonic ConvertFrom(UltrasonicData data)
        {
            return new Lgsvl.Ultrasonic()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                minimum_distance = data.MinimumDistance,
            };
        }

        public static Ros.NavSatFix ConvertFrom(GpsData data)
        {
            return new Ros.NavSatFix()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                status = new Ros.NavSatStatus()
                {
                    status = Ros.NavFixStatus.STATUS_FIX,
                    service = Ros.GpsServisType.SERVICE_GPS,
                },
                latitude = data.Latitude,
                longitude = data.Longitude,
                altitude = data.Altitude,

                position_covariance = new double[]
                {
                    0.0001, 0, 0,
                    0, 0.0001, 0,
                    0, 0, 0.0001,
                },

                position_covariance_type = Ros.CovarianceType.COVARIANCE_TYPE_DIAGONAL_KNOWN
            };
        }

        public static Ros.Odometry ConvertFrom(GpsOdometryData data)
        {
            return new Ros.Odometry()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },
                child_frame_id = data.ChildFrame,
                pose = new Ros.PoseWithCovariance()
                {
                    pose = new Ros.Pose()
                    {
                        position = new Ros.Point()
                        {
                            x = data.Easting,
                            y = data.Northing,
                            z = data.Altitude,
                        },
                        orientation = Convert(data.Orientation),
                    },
                    covariance = new double[]
                    {
                        0.0001, 0, 0, 0, 0, 0,
                        0, 0.0001, 0, 0, 0, 0,
                        0, 0, 0.0001, 0, 0, 0,
                        0, 0, 0, 0.0001, 0, 0,
                        0, 0, 0, 0, 0.0001, 0,
                        0, 0, 0, 0, 0, 0.0001
                    }
                },
                twist = new Ros.TwistWithCovariance()
                {
                    twist = new Ros.Twist()
                    {
                        linear = new Ros.Vector3()
                        {
                            x = data.ForwardSpeed,
                            y = 0.0,
                            z = 0.0,
                        },
                        angular = new Ros.Vector3()
                        {
                            x = 0.0,
                            y = 0.0,
                            z = -data.AngularVelocity.y,
                        }
                    },
                    covariance = new double[]
                    {
                        0.0001, 0, 0, 0, 0, 0,
                        0, 0.0001, 0, 0, 0, 0,
                        0, 0, 0.0001, 0, 0, 0,
                        0, 0, 0, 0.0001, 0, 0,
                        0, 0, 0, 0, 0.0001, 0,
                        0, 0, 0, 0, 0, 0.0001
                    }
                }
            };
        }

        // public static VehicleOdometry ConvertFrom(VehicleOdometryData data)
        // {
        //     return new VehicleOdometry()
        //     {
        //         stamp = Convert(data.Time),
        //         velocity_mps = data.Speed,
        //         front_wheel_angle_rad = UnityEngine.Mathf.Deg2Rad * data.SteeringAngleFront,
        //         rear_wheel_angle_rad = UnityEngine.Mathf.Deg2Rad * data.SteeringAngleBack,
        //     };
        // }

        public static Detected3DObjectArray ConvertTo(Lgsvl.Detection3DArray data)
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

        public static VehicleControlData ConvertTo(Lgsvl.VehicleControlData data)
        {
            float Deg2Rad = UnityEngine.Mathf.Deg2Rad;
            float MaxSteeringAngle = 39.4f * Deg2Rad;
            float wheelAngle = 0f;

            if (data.target_wheel_angle > MaxSteeringAngle)
            {
                wheelAngle = MaxSteeringAngle;
            }
            else if (data.target_wheel_angle < -MaxSteeringAngle)
            {
                wheelAngle = -MaxSteeringAngle;
            }

            // ratio between -MaxSteeringAngle and MaxSteeringAngle
            var k = (float)(wheelAngle + MaxSteeringAngle) / (MaxSteeringAngle*2);

            // target_wheel_angular_rate, target_gear are not supported on simulator side.

            return new VehicleControlData()
            {
                TimeStampSec = Convert(data.header.stamp),
                Acceleration = data.acceleration_pct,
                Braking = data.braking_pct,
                SteerAngle = UnityEngine.Mathf.Lerp(-1f, 1f, k),

            };
        }

        public static VehicleStateData ConvertTo(Lgsvl.VehicleStateData data)
        {
            return new VehicleStateData()
            {
                Time = Convert(data.header.stamp),
                Blinker = (byte) data.blinker_state,
                HeadLight = (byte) data.headlight_state,
                Wiper = (byte) data.wiper_state,
                Gear = (byte) data.current_gear,
                Mode = (byte) data.vehicle_mode,
                HandBrake = data.hand_brake_active,
                Horn = data.horn_active,
                Autonomous = data.autonomous_mode_active,
            };
        }

        public static Ros.Imu ConvertFrom(ImuData data)
        {
            return new Ros.Imu()
            {
                header = new Ros.Header()
                {
                    stamp = Convert(data.Time),
                    frame_id = data.Frame,
                },

                orientation = Convert(data.Orientation),
                orientation_covariance = new double[9] { 0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001 },
                angular_velocity = ConvertToVector(data.AngularVelocity),
                angular_velocity_covariance = new double[9] { 0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001 },
                linear_acceleration = ConvertToVector(data.Acceleration),
                linear_acceleration_covariance = new double[9] { 0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001 },
            };
        }

        public static Ros.Clock ConvertFrom(ClockData data)
        {
            return new Ros.Clock()
            {
                clock = Convert(data.Clock),
            };
        }

        static Ros.Point ConvertToPoint(UnityEngine.Vector3 v)
        {
            return new Ros.Point() { x = v.x, y = v.y, z = v.z };
        }

        static Ros.Point ConvertToPoint(double3 d)
        {
            return new Ros.Point() { x = d.x, y = d.y, z = d.z };
        }

        static Ros.Vector3 ConvertToVector(UnityEngine.Vector3 v)
        {
            return new Ros.Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static Ros.Quaternion Convert(UnityEngine.Quaternion q)
        {
            return new Ros.Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
        }

        static UnityEngine.Vector3 Convert(Ros.Point p)
        {
            return new UnityEngine.Vector3((float)p.x, (float)p.y, (float)p.z);
        }

        static UnityEngine.Vector3 Convert(Ros.Vector3 v)
        {
            return new UnityEngine.Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        static UnityEngine.Quaternion Convert(Ros.Quaternion q)
        {
            return new UnityEngine.Quaternion((float)q.x, (float)q.y, (float)q.z, (float)q.w);
        }

        public static Ros.Time Convert(double unixEpochSeconds)
        {
            long nanosec = (long)(unixEpochSeconds * 1e9);

            return new Ros.Time()
            {
                secs = (int)(nanosec / 1000000000),
                nsecs = (uint)(nanosec % 1000000000),
            };
        }

        public static double Convert(Ros.Time time)
        {
            return (double)time.secs + (double)time.nsecs * 1e-9;
        }
    }
}
