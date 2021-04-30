/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge.Data;
using System.Text;
using Unity.Mathematics;

namespace Simulator.Bridge.Cyber
{
    static class CyberConversions
    {
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
                        error_code = apollo.common.ErrorCode.Ok,
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
                    lidar_timestamp = (ulong)(data.Time * 1e9),
                    version = 1,
                    status = new apollo.common.StatusPb()
                    {
                        error_code = apollo.common.ErrorCode.Ok,
                    },
                    frame_id = data.Frame,
                },
                frame_id = data.Frame,
                is_dense = false,
                measurement_time = data.Time,
                height = 1,
                width = (uint)data.PointCount, // TODO is this right?
            };

            for (int i = 0; i < data.PointCount; i++)
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

        public static apollo.perception.TrafficLightDetection ConvertFrom(SignalDataArray data)
        {
            bool contain_lights = false;
            if (data.Data.Length > 0)
            {
                contain_lights = true;
            }

            var signals = new apollo.perception.TrafficLightDetection()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    sequence_num = data.Sequence,
                    camera_timestamp = (ulong)(data.Time * 1e9),
                },
                contain_lights = contain_lights,
            };

            foreach (SignalData d in data.Data)
            {
                var color = apollo.perception.TrafficLight.Color.Black;
                if (d.Label == "green")
                {
                    color = apollo.perception.TrafficLight.Color.Green;
                }
                else if (d.Label == "yellow")
                {
                    color = apollo.perception.TrafficLight.Color.Yellow;
                }
                else if (d.Label == "red")
                {
                    color = apollo.perception.TrafficLight.Color.Red;
                }

                signals.traffic_light.Add
                (
                    new apollo.perception.TrafficLight()
                    {
                        color = color,
                        id = d.Id,
                        confidence = 1.0,
                        blink = false,
                    }
                );
            }

            return signals;
        }

        public static apollo.perception.PerceptionObstacles ConvertFrom(Detected3DObjectData data)
        {
            var obstacles = new apollo.perception.PerceptionObstacles()
            {
                header = new apollo.common.Header()
                {
                    timestamp_sec = data.Time,
                    module_name = "perception_obstacle",
                    sequence_num = data.Sequence,
                    lidar_timestamp = (ulong)(data.Time * 1e9),
                },
                error_code = apollo.common.ErrorCode.Ok,
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

                apollo.perception.PerceptionObstacle.Type type = apollo.perception.PerceptionObstacle.Type.Unknown;
                apollo.perception.PerceptionObstacle.SubType subType = apollo.perception.PerceptionObstacle.SubType.StUnknown;
                if (d.Label == "Pedestrian")
                {
                    type = apollo.perception.PerceptionObstacle.Type.Pedestrian;
                    subType = apollo.perception.PerceptionObstacle.SubType.StPedestrian;
                }
                else if (d.Label == "BoxTruck")
                {
                    type = apollo.perception.PerceptionObstacle.Type.Vehicle;
                    subType = apollo.perception.PerceptionObstacle.SubType.StTruck;
                }
                else if (d.Label == "SchoolBus")
                {
                    type = apollo.perception.PerceptionObstacle.Type.Vehicle;
                    subType = apollo.perception.PerceptionObstacle.SubType.StBus;
                }
                else
                {
                    type = apollo.perception.PerceptionObstacle.Type.Vehicle;
                    subType = apollo.perception.PerceptionObstacle.SubType.StCar;
                }

                var po = new apollo.perception.PerceptionObstacle()
                {
                    id = (int)d.Id,
                    position = ConvertToPoint(d.Gps),
                    theta = (90 - d.Heading) * Mathf.Deg2Rad,
                    velocity = ConvertToPoint(velocity),
                    width = size.x,
                    length = size.y,
                    height = size.z,
                    tracking_time = d.TrackingTime,
                    type = type,
                    timestamp = data.Time,
                    acceleration = ConvertToPoint(acceleration),
                    anchor_point = ConvertToPoint(d.Gps),
                    sub_type = subType,
                };

                // polygon points := obstacle corner points
                var cx = d.Gps.Easting;
                var cy = d.Gps.Northing;
                var cz = d.Gps.Altitude;
                var px = 0.5f * size.x;
                var py = 0.5f * size.y;
                var c = Mathf.Cos((float)-d.Heading * Mathf.Deg2Rad);
                var s = Mathf.Sin((float)-d.Heading * Mathf.Deg2Rad);

                var p1 = new apollo.common.Point3D(){ x = -px * c + py * s + cx, y = -px * s - py * c + cy, z = cz };
                var p2 = new apollo.common.Point3D(){ x = px * c + py * s + cx, y = px * s - py * c + cy, z = cz };
                var p3 = new apollo.common.Point3D(){ x = px * c - py * s + cx, y = px * s + py * c + cy, z = cz };
                var p4 = new apollo.common.Point3D(){ x = -px * c - py * s + cx, y = -px * s + py * c + cy, z = cz };
                po.polygon_point.Add(p1);
                po.polygon_point.Add(p2);
                po.polygon_point.Add(p3);
                po.polygon_point.Add(p4);

                po.measurements.Add(new apollo.perception.SensorMeasurement()
                {
                    sensor_id = "velodyne128",
                    id = (int)d.Id,
                    position = ConvertToPoint(d.Gps),
                    theta = (90 - d.Heading) * Mathf.Deg2Rad,
                    width = size.x,
                    length = size.y,
                    height = size.z,
                    velocity = ConvertToPoint(velocity),
                    type = type,
                    sub_type = subType,
                    timestamp = data.Time,
                });

                obstacles.perception_obstacle.Add(po);
            }

            return obstacles;
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

        public static apollo.drivers.ContiRadar ConvertFrom(DetectedRadarObjectData data)
        {
            var r = new apollo.drivers.ContiRadar()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                    module_name = "conti_radar",
                },
                object_list_status = new apollo.drivers.ObjectListStatus_60A
                {
                    nof_objects = data.Data.Length,
                    meas_counter = 22800,
                    interface_version = 0
                }
            };

            foreach (var obj in data.Data)
            {
                r.contiobs.Add(new apollo.drivers.ContiRadarObs()
                {
                    header = r.header,
                    clusterortrack = false,
                    obstacle_id = (int)obj.Id,
                    longitude_dist = Vector3.Project(obj.RelativePosition, obj.SensorAim).magnitude,
                    lateral_dist = Vector3.Project(obj.RelativePosition, obj.SensorRight).magnitude * (Vector3.Dot(obj.RelativePosition, obj.SensorRight) > 0 ? -1 : 1),
                    longitude_vel = Vector3.Project(obj.RelativeVelocity, obj.SensorAim).magnitude * (Vector3.Dot(obj.RelativeVelocity, obj.SensorAim) > 0 ? -1 : 1),
                    lateral_vel = Vector3.Project(obj.RelativeVelocity, obj.SensorRight).magnitude * (Vector3.Dot(obj.RelativeVelocity, obj.SensorRight) > 0 ? -1 : 1),
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

        public static apollo.canbus.Chassis ConvertFrom(CanBusData data)
        {
            var orientation = ConvertToRfu(data.Orientation);
            var eul = orientation.eulerAngles;

            float dir;
            if (eul.z >= 0) dir = 45 * Mathf.Round((eul.z % 360) / 45.0f);
            else dir = 45 * Mathf.Round((eul.z % 360 + 360) / 45.0f);

            var measurement_time = GpsUtils.UtcSecondsToGpsSeconds(data.Time);
            var gpsTime = DateTimeOffset.FromUnixTimeSeconds((long)measurement_time).DateTime.ToLocalTime();

            return new apollo.canbus.Chassis()
            {
                header = new apollo.common.Header()
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
                throttle_percentage = data.Throttle * 100.0f,
                brake_percentage = data.Braking * 100.0f,
                steering_percentage = -data.Steering * 100,
                parking_brake = data.ParkingBrake,
                //high_beam_signal = data.HighBeamSignal,
                //low_beam_signal = data.LowBeamSignal,
                //left_turn_signal = data.LeftTurnSignal,
                //right_turn_signal = data.RightTurnSignal,
                wiper = data.Wipers,
                driving_mode = apollo.canbus.Chassis.DrivingMode.CompleteAutoDrive,
                gear_location = data.InReverse ? apollo.canbus.Chassis.GearPosition.GearReverse : apollo.canbus.Chassis.GearPosition.GearDrive,

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
                    heading = eul.z,
                    hdop = 0.1,
                    vdop = 0.1,
                    quality = apollo.canbus.GpsQuality.Fix3d,
                    num_satellites = 15,
                    gps_speed = data.Velocity.magnitude,
                }
            };
        }

        public static apollo.drivers.gnss.GnssBestPose ConvertFrom(GpsData data)
        {
            float Accuracy = 0.01f; // just a number to report
            double Height = 0; // sea level to WGS84 ellipsoid

            return new apollo.drivers.gnss.GnssBestPose()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                },
                measurement_time = GpsUtils.UtcSecondsToGpsSeconds(data.Time),
                sol_status = apollo.drivers.gnss.SolutionStatus.SolComputed,
                sol_type = apollo.drivers.gnss.SolutionType.NarrowInt,

                latitude = data.Latitude,  // in degrees
                longitude = data.Longitude,  // in degrees
                height_msl = Height,  // height above mean sea level in meters
                undulation = 0,  // undulation = height_wgs84 - height_msl
                datum_id = apollo.drivers.gnss.DatumId.Wgs84,  // datum id number
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
            var orientation = ConvertToRfu(data.Orientation);
            float yaw = orientation.eulerAngles.z;

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
                        x = data.Easting,  // East from the origin, in meters.
                        y = data.Northing,  // North from the origin, in meters.
                        z = data.Altitude// Up from the WGS-84 ellipsoid, in meters.
                    },

                    // A quaternion that represents the rotation from the IMU coordinate
                    // (Right/Forward/Up) to the world coordinate (East/North/Up).
                    orientation = Convert(orientation),

                    // Linear velocity of the VRP in the map reference frame.
                    // East/north/up in meters per second.
                    linear_velocity = new apollo.common.Point3D()
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

        public static apollo.perception.PerceptionLanes ConvertFrom(LaneLinesData data)
        {
            var result = new apollo.perception.PerceptionLanes()
            {
                header = new apollo.common.Header()
                {
                    sequence_num = data.Sequence,
                    frame_id = data.Frame,
                    timestamp_sec = data.Time,
                }
            };

            foreach (var lineData in data.lineData)
            {
                var line = new apollo.perception.camera.CameraLaneLine()
                {
                    curve_camera_coord = Convert(lineData.CurveCameraCoord)
                };

                // Note: Don't cast one enum value to another in case Apollo changes their underlying values
                switch (lineData.PositionType)
                {
                    case LaneLinePositionType.BollardLeft:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.BollardLeft;
                        break;
                    case LaneLinePositionType.FourthLeft:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.FourthLeft;
                        break;
                    case LaneLinePositionType.ThirdLeft:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.ThirdLeft;
                        break;
                    case LaneLinePositionType.AdjacentLeft:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.AdjacentLeft;
                        break;
                    case LaneLinePositionType.EgoLeft:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.EgoLeft;
                        break;
                    case LaneLinePositionType.EgoRight:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.EgoRight;
                        break;
                    case LaneLinePositionType.AdjacentRight:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.AdjacentRight;
                        break;
                    case LaneLinePositionType.ThirdRight:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.ThirdRight;
                        break;
                    case LaneLinePositionType.FourthRight:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.FourthRight;
                        break;
                    case LaneLinePositionType.BollardRight:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.BollardRight;
                        break;
                    case LaneLinePositionType.Other:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.Other;
                        break;
                    case LaneLinePositionType.Unknown:
                        line.pos_type = apollo.perception.camera.LaneLinePositionType.Unknown;
                        break;
                }

                // Note: Don't cast one enum value to another in case Apollo changes their underlying values
                switch (lineData.Type)
                {
                    case LaneLineType.WhiteDashed:
                        line.type = apollo.perception.camera.LaneLineType.WhiteDashed;
                        break;
                    case LaneLineType.WhiteSolid:
                        line.type = apollo.perception.camera.LaneLineType.WhiteSolid;
                        break;
                    case LaneLineType.YellowDashed:
                        line.type = apollo.perception.camera.LaneLineType.YellowDashed;
                        break;
                    case LaneLineType.YellowSolid:
                        line.type = apollo.perception.camera.LaneLineType.YellowSolid;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                result.camera_laneline.Add(line);
            }

            return result;
        }

        public static Detected3DObjectArray ConvertTo(apollo.perception.PerceptionObstacles data)
        {
            var detections = new List<Detected3DObject>();

            foreach (var obstacle in data.perception_obstacle)
            {
                String label;
                switch (obstacle.type)
                {
                    case apollo.perception.PerceptionObstacle.Type.Vehicle:
                        label = "Car";
                        break;
                    case apollo.perception.PerceptionObstacle.Type.Pedestrian:
                        label = "Pedestrian";
                        break;
                    case apollo.perception.PerceptionObstacle.Type.Bicycle:
                        label = "Bicycle";
                        break;
                    default:
                        label = "Unknown";
                        break;
                }

                GpsData gps = new GpsData()
                {
                    Easting = obstacle.position.x,
                    Northing = obstacle.position.y,
                    Altitude = obstacle.position.z,
                };

                var det = new Detected3DObject()
                {
                    Id = (uint)obstacle.id,
                    Label = label,
                    Gps = gps,
                    Heading = 90 - obstacle.theta * Mathf.Rad2Deg,
                    Scale = new Vector3
                    (
                        (float)obstacle.width,
                        (float)obstacle.height,
                        (float)obstacle.length
                    ),
                };

                detections.Add(det);
            }

            return new Detected3DObjectArray()
            {
                Data = detections.ToArray(),
            };
        }

        public static VehicleControlData ConvertTo(apollo.control.ControlCommand data)
        {
            var vehicleControlData = new VehicleControlData()
            {
                Acceleration = (float)data.throttle / 100,
                Braking = (float)data.brake / 100,
                SteerRate = (float)data.steering_rate,
                SteerTarget = (float)data.steering_target / 100,
                TimeStampSec = data.header.timestamp_sec,
            };

            switch (data.gear_location)
            {
                case global::apollo.canbus.Chassis.GearPosition.GearNeutral:
                    vehicleControlData.CurrentGear = GearPosition.Neutral;
                    break;
                case global::apollo.canbus.Chassis.GearPosition.GearDrive:
                    vehicleControlData.CurrentGear = GearPosition.Drive;
                    break;
                case global::apollo.canbus.Chassis.GearPosition.GearReverse:
                    vehicleControlData.CurrentGear = GearPosition.Reverse;
                    break;
                case global::apollo.canbus.Chassis.GearPosition.GearParking:
                    vehicleControlData.CurrentGear = GearPosition.Parking;
                    break;
                case global::apollo.canbus.Chassis.GearPosition.GearLow:
                    vehicleControlData.CurrentGear = GearPosition.Low;
                    break;
            }
            return vehicleControlData;
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

                measurement_time = GpsUtils.UtcSecondsToGpsSeconds(data.Time),
                measurement_span = (float)data.MeasurementSpan,
                linear_acceleration = ConvertToPoint(new Vector3(data.Acceleration.x, data.Acceleration.y, data.Acceleration.z)),
                angular_velocity = ConvertToPoint(new Vector3(data.AngularVelocity.x, data.AngularVelocity.y, data.AngularVelocity.z)),
            };
        }

        public static apollo.localization.CorrectedImu ConvertFrom(CorrectedImuData data)
        {
            var orientation = ConvertToRfu(data.Orientation);
            var angles = orientation.eulerAngles;
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
                    linear_acceleration = ConvertToPoint(new Vector3(data.GlobalAcceleration.x, data.GlobalAcceleration.y, data.GlobalAcceleration.z)),
                    angular_velocity = ConvertToPoint(new Vector3(data.GlobalAngularVelocity.x, data.GlobalAngularVelocity.y, data.GlobalAngularVelocity.z)),
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

        public static apollo.cyber.proto.Clock ConvertFrom(ClockData data)
        {
            return new apollo.cyber.proto.Clock()
            {
                clock = (ulong)(data.Clock * 1e9)
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

        static apollo.common.Point3D ConvertToPoint(double3 v)
        {
            return new apollo.common.Point3D() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Point3D ConvertToPoint(GpsData g)
        {
            return new apollo.common.Point3D() { x = g.Easting, y = g.Northing, z = g.Altitude };
        }

        static apollo.common.Vector3 ConvertToVector(Vector3 v)
        {
            return new apollo.common.Vector3() { x = v.x, y = v.y, z = v.z };
        }

        static apollo.common.Quaternion Convert(Quaternion q)
        {
            return new apollo.common.Quaternion() { qx = q.x, qy = q.y, qz = q.z, qw = q.w };
        }
        
        static apollo.perception.camera.LaneLineCubicCurve Convert(LaneLineCubicCurve c)
        {
            return new apollo.perception.camera.LaneLineCubicCurve()
            {
                a = c.C0,
                b = c.C1,
                c = c.C2,
                d = c.C3,
                longitude_max = c.MaxX,
                longitude_min = c.MinX
            };
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

        static Quaternion ConvertToRfu(Quaternion q)
        {
            return q * new Quaternion(0f, 0f, -0.7071068f, 0.7071068f);
        }
    }
}