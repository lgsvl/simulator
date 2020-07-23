/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Simulator.Bridge.Data;
using Unity.Mathematics;
// NOTE: DO NOT add using "Ros.Ros", "Ros.Apollo" or "Ros.Lgsvl" namespaces here to avoid
// NOTE: confusion between types. Keep them fully qualified in this file.

namespace Simulator.Bridge.Ros
{
    public static class Conversions
    {
        public static Ros.CompressedImage ConvertFrom(ImageData data)
        {
            return new Ros.CompressedImage()
            {
                header = new Ros.Header()
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

        public static Lgsvl.Detection2DArray ConvertFrom(Detected2DObjectData data)
        {
            return new Lgsvl.Detection2DArray()
            {
                header = new Ros.Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                detections = data.Data.Select(d => new Lgsvl.Detection2D()
                {
                    id = d.Id,
                    label = d.Label,
                    score = d.Score,
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
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
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
                    score = d.Score,
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

        public static Apollo.Perception.PerceptionObstacles ApolloConvertFrom(Detected3DObjectData data)
        {
            var obstacles = new Apollo.Perception.PerceptionObstacles()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = data.Time,
                    module_name = "perception_obstacle",
                    sequence_num = data.Sequence,
                    lidar_timestamp = (ulong)(data.Time * 1e9),
                },
                error_code = Apollo.Common.ErrorCode.OK,
                perception_obstacle = new List<Apollo.Perception.PerceptionObstacle>(),
            };

            foreach (var d in data.Data)
            {
                // Transform from (Right/Up/Forward) to (Right/Forward/Up)
                var velocity = d.Velocity;
                velocity.Set(velocity.x, velocity.z, velocity.y);

                var acceleration = d.Acceleration;
                acceleration.Set(acceleration.x, acceleration.z, acceleration.y);

                var size = d.Scale;
                size.Set(size.x, size.z, size.y);

                Apollo.Perception.Type type = Apollo.Perception.Type.UNKNOWN;
                if (d.Label == "Pedestrian")
                {
                    type = Apollo.Perception.Type.PEDESTRIAN;
                }
                else
                {
                    type = Apollo.Perception.Type.VEHICLE;
                }

                var po = new Apollo.Perception.PerceptionObstacle()
                {
                    id = (int)d.Id,
                    position = ConvertToApolloPoint(d.Gps),
                    theta = (90 - d.Heading) * UnityEngine.Mathf.Deg2Rad,
                    velocity = ConvertToApolloPoint(velocity),
                    width = size.x,
                    length = size.y,
                    height = size.z,
                    polygon_point = new List<Apollo.Point3D>(),
                    tracking_time = d.TrackingTime,
                    type = type,
                    timestamp = data.Time,
                };

                // polygon points := obstacle corner points
                var cx = d.Gps.Easting;
                var cy = d.Gps.Northing;
                var cz = d.Gps.Altitude;
                var px = 0.5f * size.x;
                var py = 0.5f * size.y;
                var c = UnityEngine.Mathf.Cos((float)-d.Heading * UnityEngine.Mathf.Deg2Rad);
                var s = UnityEngine.Mathf.Sin((float)-d.Heading * UnityEngine.Mathf.Deg2Rad);

                var p1 = new Apollo.Point3D(){ x = -px * c + py * s + cx, y = -px * s - py * c + cy, z = cz };
                var p2 = new Apollo.Point3D(){ x = px * c + py * s + cx, y = px * s - py * c + cy, z = cz };
                var p3 = new Apollo.Point3D(){ x = px * c - py * s + cx, y = px * s + py * c + cy, z = cz };
                var p4 = new Apollo.Point3D(){ x = -px * c - py * s + cx, y = -px * s + py * c + cy, z = cz };
                po.polygon_point.Add(p1);
                po.polygon_point.Add(p2);
                po.polygon_point.Add(p3);
                po.polygon_point.Add(p4);

                obstacles.perception_obstacle.Add(po);
            }

            return obstacles;
        }

        public static Lgsvl.SignalArray ConvertFrom(SignalDataArray data)
        {
            return new Lgsvl.SignalArray()
            {
                header = new Ros.Header()
                {
                    seq = data.Sequence,
                    stamp = Conversions.ConvertTime(data.Time),
                    frame_id = data.Frame,
                },
                signals = data.Data.Select(d => new Lgsvl.Signal()
                {
                    id = d.SeqId,
                    label = d.Label,
                    score = d.Score,
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

        public static Apollo.Perception.TrafficLightDetection ApolloConvertFrom(SignalDataArray data)
        {
            bool contain_lights = false;
            if (data.Data.Length > 0)
            {
                contain_lights = true;
            }

            var signals = new Apollo.Perception.TrafficLightDetection()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                    camera_timestamp = (ulong)(data.Time * 1e9),
                },
                contain_lights = contain_lights,
                traffic_light = new List<Apollo.Perception.TrafficLight>(),
            };

            foreach (SignalData d in data.Data)
            {
                var color = Apollo.Perception.Color.BLACK;
                if (d.Label == "green")
                {
                    color = Apollo.Perception.Color.GREEN;
                }
                else if (d.Label == "yellow")
                {
                    color = Apollo.Perception.Color.YELLOW;
                }
                else if (d.Label == "red")
                {
                    color = Apollo.Perception.Color.RED;
                }

                signals.traffic_light.Add
                (
                    new Apollo.Perception.TrafficLight()
                    {
                        color = color,
                        id = d.Id,
                        confidence = 1.0,
                    }
                );
            }

            return signals;
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

        public static Lgsvl.DetectedRadarObjectArray ROS2ConvertFrom(DetectedRadarObjectData data)
        {
            var r = new Lgsvl.DetectedRadarObjectArray()
            {
                header = new Ros.Header()
                {
                    stamp = ConvertTime(data.Time),
                    seq = data.Sequence,
                    frame_id = data.Frame,
                },
            };

            foreach (var obj in data.Data)
            {
                r.objects.Add(new Lgsvl.DetectedRadarObject()
                {
                    sensor_aim = ConvertToRosVector3(obj.SensorAim),
                    sensor_right = ConvertToRosVector3(obj.SensorRight),
                    sensor_position = ConvertToRosPoint(obj.SensorPosition),
                    sensor_velocity = ConvertToRosVector3(obj.SensorVelocity),
                    sensor_angle = obj.SensorAngle,
                    object_position = ConvertToRosPoint(obj.Position),
                    object_velocity = ConvertToRosVector3(obj.Velocity),
                    object_relative_position = ConvertToRosPoint(obj.RelativePosition),
                    object_relative_velocity = ConvertToRosVector3(obj.RelativeVelocity),
                    object_collider_size = ConvertToRosVector3(obj.ColliderSize),
                    object_state = (byte)obj.State,
                    new_detection = obj.NewDetection,
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

            var measurement_time = GpsUtils.UtcSecondsToGpsSeconds(data.Time);
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

            var measurement_time = GpsUtils.UtcSecondsToGpsSeconds(data.Time);

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

        public static Ros.Odometry ConvertFrom(GpsOdometryData data)
        {
            return new Ros.Odometry()
            {
                header = new Ros.Header()
                {
                    stamp = ConvertTime(data.Time),
                    seq = data.Sequence,
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
                            z = - data.AngularVelocity.y,
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

        public static Apollo.Gps ApolloConvertFrom(GpsOdometryData data)
        {
            var orientation = ConvertToRfu(data.Orientation);
            float yaw = orientation.eulerAngles.z;

            return new Apollo.Gps()
            {
                header = new Apollo.Header()
                {
                    timestamp_sec = GpsUtils.UtcSecondsToGpsSeconds(data.Time),
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
                    orientation = ConvertApolloQuaternion(orientation),

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
                TimeStampSec = ConvertTime(data.header.stamp),
                Acceleration = (float)data.ctrl_cmd.linear_acceleration > 0 ? (float)data.ctrl_cmd.linear_acceleration : 0f,
                Braking = (float)data.ctrl_cmd.linear_acceleration < 0 ? -(float)data.ctrl_cmd.linear_acceleration : 0f,
                Velocity = (float)data.twist_cmd.twist.linear.x,
                SteerAngularVelocity = (float)data.twist_cmd.twist.angular.z,
                SteerAngle = (float)data.ctrl_cmd.steering_angle,
                ShiftGearUp = shiftUp,
                ShiftGearDown = shiftDown,
            };
        }

        public static VehicleControlData ConvertTo(Autoware.VehicleControlCommand data)
        {
            return new VehicleControlData()
            {
                TimeStampSec = ConvertTime(data.stamp),
                Acceleration = data.long_accel_mps2,
                SteerAngle = data.front_wheel_angle_rad,
            };
        }

        public static VehicleControlData ConvertTo(Lgsvl.VehicleControlDataRos data)
        {
            float Deg2Rad = UnityEngine.Mathf.Deg2Rad;
            float MaxSteeringAngle = 39.4f * Deg2Rad;
            float wheelAngle = 0f;
            if (data.target_wheel_angle > MaxSteeringAngle)
                wheelAngle = MaxSteeringAngle;
            else if (data.target_wheel_angle < -MaxSteeringAngle)
                wheelAngle = -MaxSteeringAngle;

            // ratio between -MaxSteeringAngle and MaxSteeringAngle
            var k = (float)(wheelAngle + MaxSteeringAngle) / (MaxSteeringAngle*2);

            // target_wheel_angular_rate, target_gear are not supported on simulator side.
            return new VehicleControlData()
            {
                Acceleration = data.acceleration_pct,
                Braking = data.braking_pct,
                SteerAngle = UnityEngine.Mathf.Lerp(-1f, 1f, k),
            };
        }

        public static VehicleStateData ConvertTo(Lgsvl.VehicleStateDataRos data)
        {
            return new VehicleStateData()
            {
                Time = ConvertTime(data.header.stamp),
                Blinker = (byte)data.blinker_state,
                HeadLight = (byte)data.headlight_state,
                Wiper = (byte)data.wiper_state,
                Gear = (byte)data.current_gear,
                Mode = (byte)data.vehicle_mode,
                HandBrake = data.hand_brake_active,
                Horn = data.horn_active,
                Autonomous = data.autonomous_mode_active,
            };
        }

        public static VehicleControlData ConvertTo(Apollo.control_command data)
        {
            return new VehicleControlData()
            {
                Acceleration = (float)data.throttle / 100,
                Braking = (float)data.brake / 100,
                SteerRate = (float)data.steering_rate,
                SteerTarget = (float)data.steering_target / 100,
                TimeStampSec = (float)data.header.timestamp_sec,
            };
        }

        public static Ros.Imu ConvertFrom(ImuData data)
        {
            return new Ros.Imu()
            {
                header = new Ros.Header()
                {
                    stamp = ConvertTime(data.Time),
                    seq = data.Sequence,
                    frame_id = data.Frame,
                },

                orientation = Convert(data.Orientation),
                orientation_covariance = new double[9]{0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001},
                angular_velocity = ConvertToVector(data.AngularVelocity),
                angular_velocity_covariance = new double[9]{0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001},
                linear_acceleration = ConvertToVector(data.Acceleration),
                linear_acceleration_covariance = new double[9]{0.0001, 0, 0, 0, 0.0001, 0, 0, 0, 0.0001},
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
                linear_acceleration = new Apollo.Point3D() { x = data.Acceleration.x, y = data.Acceleration.y, z = data.Acceleration.z },
                angular_velocity = new Apollo.Point3D() { x = data.AngularVelocity.x, y = data.AngularVelocity.y, z = data.AngularVelocity.z },
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
                    linear_acceleration = new Apollo.Point3D() { x = data.Acceleration.x, y = data.Acceleration.y, z = -data.Acceleration.z },
                    angular_velocity = new Apollo.Point3D() { x = data.AngularVelocity.x, y = data.AngularVelocity.y, z = data.AngularVelocity.z },
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

        public static Ros.Clock ConvertFrom(ClockData data)
        {
            return new Ros.Clock()
            {
                clock = ConvertTime(data.Clock),
            };
        }

        public static VehicleControlData ConvertTo(Ros.TwistStamped data)
        {
            return new VehicleControlData()
            {
                SteerInput = (float)data.twist.angular.x,
            };
        }

        public static EmptySrv ConvertTo(Ros.Empty data)
        {
            return new EmptySrv();
        }

        public static Ros.Empty ConvertFrom(EmptySrv data)
        {
            return new Ros.Empty();
        }

        public static SetBoolSrv ConvertTo(Ros.SetBool data)
        {
            return new SetBoolSrv()
            {
                data = data.data,
            };
        }

        public static Ros.SetBoolResponse ConvertFrom(SetBoolSrv data)
        {
            return new Ros.SetBoolResponse()
            {
                success = data.data,
                message = data.message,
            };
        }

        public static Ros.Trigger ConvertFrom(TriggerSrv data)
        {
            return new Ros.Trigger()
            {
                success = data.data,
                message = data.message,
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

        static Apollo.Point3D ConvertToApolloPoint(UnityEngine.Vector3 v)
        {
            return new Apollo.Point3D() { x = v.x, y = v.y, z = v.z };
        }

        static Apollo.Point3D ConvertToApolloPoint(GpsData g)
        {
            return new Apollo.Point3D() { x = g.Easting, y = g.Northing, z = g.Altitude };
        }

        static Ros.Vector3 ConvertToVector(UnityEngine.Vector3 v)
        {
            return new Ros.Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static Ros.Vector3 ConvertToRosVector3(UnityEngine.Vector3 v)
        {
            return new Ros.Vector3() { x = v.z, y = -v.x, z = v.y };
        }

        static Ros.Point ConvertToRosPoint(UnityEngine.Vector3 v)
        {
            return new Ros.Point() { x = v.z, y = -v.x, z = v.y };
        }

        static Ros.Quaternion Convert(UnityEngine.Quaternion q)
        {
            return new Ros.Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
        }

        static Apollo.Quaternion ConvertApolloQuaternion(UnityEngine.Quaternion q)
        {
            return new Apollo.Quaternion() { qx = q.x, qy = q.y, qz = q.z, qw = q.w };
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

        static UnityEngine.Quaternion ConvertToRfu(UnityEngine.Quaternion q)
        {
            // In Righthanded xyz, rotate by -90 deg around z axis.
            return q * UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.forward);
        }

        public static Ros.Time ConvertTime(double unixEpochSeconds)
        {
            long nanosec = (long)(unixEpochSeconds * 1e9);

            return new Ros.Time()
            {
                secs = nanosec / 1000000000,
                nsecs = (uint)(nanosec % 1000000000),
            };
        }

        public static double ConvertTime(Ros.Time t)
        {
            double time = (double)t.secs + (double)t.nsecs / 1000000000;

            return time;
        }
    }
}
