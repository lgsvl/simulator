/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    static class DefaultSensors
    {
        public static readonly string Autoware = "[" + string.Join(",",
            @"{""type"": ""GPS Device"", ""name"": ""GPS"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/nmea_sentence"", ""Frame"": ""gps"", ""IgnoreMapOrigin"": true},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": 0, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS Odometry"", ""name"": ""GPS Odometry"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/odom"", ""Frame"": ""gps"", ""IgnoreMapOrigin"": true},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": 0, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""IMU"", ""name"": ""IMU"",
            ""params"": {""Topic"": ""/imu_raw"", ""Frame"": ""imu""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": 0, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Lidar"", ""name"": ""Lidar"",
            ""params"": {""LaserCount"": 32, ""MinDistance"": 0.5, ""MaxDistance"": 100, ""RotationFrequency"": 10, ""MeasurementsPerRotation"": 360, ""FieldOfView"": 41.33, ""CenterAngle"": 10, ""Compensated"": true, ""PointColor"": ""#ff000000"", ""Topic"": ""/points_raw"", ""Frame"": ""velodyne""},
            ""transform"": {""x"": 0, ""y"": 2.312, ""z"": -0.3679201, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Color Camera"", ""name"": ""Main Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/simulator/camera_node/image/compressed"", ""Frame"": ""camera""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Keyboard Control"", ""name"": ""Keyboard Car Control""}",
            @"{""type"": ""Wheel Control"", ""name"": ""Wheel Car Control""}",
            @"{""type"": ""Vehicle Control"", ""name"": ""Autoware Car Control"",
            ""params"": {""Topic"": ""/vehicle_cmd""} }"
        ) + "]";

        public static readonly string Apollo30 = "[" + string.Join(",",
            @"{""type"": ""CAN-Bus"", ""name"": ""CAN Bus"",
            ""params"": {""Frequency"": 10, ""Topic"": ""/apollo/canbus/chassis""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": 0, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS Device"", ""name"": ""GPS"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/apollo/sensor/gnss/best_pose"", ""Frame"": ""gps""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS Odometry"", ""name"": ""GPS Odometry"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/apollo/sensor/gnss/odometry"", ""Frame"": ""gps""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""IMU"", ""name"": ""IMU"",
            ""params"": {""Topic"": ""/apollo/sensor/gnss/imu"", ""Frame"": ""imu"", ""CorrectedTopic"": ""/apollo/sensor/gnss/corrected_imu"", ""CorrectedFrame"": ""imu""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Radar"", ""name"": ""Radar"",
               ""params"": {""Frequency"": 13.4, ""Topic"": ""/apollo/sensor/conti_radar""},
               ""transform"": {""x"": 0, ""y"": 0.689, ""z"": 2.272, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Lidar"", ""name"": ""Lidar"",
            ""params"": {""LaserCount"": 32, ""MinDistance"": 0.5, ""MaxDistance"": 100, ""RotationFrequency"": 10, ""MeasurementsPerRotation"": 360, ""FieldOfView"": 41.33, ""CenterAngle"": 10, ""Compensated"": true, ""PointColor"": ""#ff000000"", ""Topic"": ""/apollo/sensor/velodyne64/compensator/PointCloud2"", ""Frame"": ""velodyne""},
            ""transform"": {""x"": 0, ""y"": 2.312, ""z"": -0.3679201, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Color Camera"", ""name"": ""Main Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/apollo/sensor/camera/traffic/image_short/compressed""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Color Camera"", ""name"": ""Telephoto Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 10, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/apollo/sensor/camera/traffic/image_long/compressed""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": -4, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Keyboard Control"", ""name"": ""Keyboard Car Control""}",
            @"{""type"": ""Wheel Control"", ""name"": ""Wheel Car Control""}",
            @"{""type"": ""Vehicle Control"", ""name"": ""Apollo Car Control"",
            ""params"": {""Topic"": ""/apollo/control""} }"
        ) + "]";

        public static readonly string Apollo50 = "[" + string.Join(",",
            @"{""type"": ""CAN-Bus"", ""name"": ""CAN Bus"",
            ""params"": {""Frequency"": 10, ""Topic"": ""/apollo/canbus/chassis""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": 0, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS Device"", ""name"": ""GPS"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/apollo/sensor/gnss/best_pose"", ""Frame"": ""gps""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS Odometry"", ""name"": ""GPS Odometry"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/apollo/sensor/gnss/odometry"", ""Frame"": ""gps""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""GPS-INS Status"", ""name"": ""GPS INS Status"",
            ""params"": {""Frequency"": 12.5, ""Topic"": ""/apollo/sensor/gnss/ins_stat"", ""Frame"": ""gps""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""IMU"", ""name"": ""IMU"",
            ""params"": {""Topic"": ""/apollo/sensor/gnss/imu"", ""Frame"": ""imu"", ""CorrectedTopic"": ""/apollo/sensor/gnss/corrected_imu"", ""CorrectedFrame"": ""imu""},
            ""transform"": {""x"": 0, ""y"": 0, ""z"": -1.348649, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Radar"", ""name"": ""Radar"",
               ""params"": {""Frequency"": 13.4, ""Topic"": ""/apollo/sensor/conti_radar""},
               ""transform"": {""x"": 0, ""y"": 0.689, ""z"": 2.272, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Lidar"", ""name"": ""Lidar"",
            ""params"": {""LaserCount"": 32, ""MinDistance"": 0.5, ""MaxDistance"": 100, ""RotationFrequency"": 10, ""MeasurementsPerRotation"": 360, ""FieldOfView"": 41.33, ""CenterAngle"": 10, ""Compensated"": true, ""PointColor"": ""#ff000000"", ""Topic"": ""/apollo/sensor/lidar128/compensator/PointCloud2"", ""Frame"": ""velodyne""},
            ""transform"": {""x"": 0, ""y"": 2.312, ""z"": -0.11, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Color Camera"", ""name"": ""Main Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/apollo/sensor/camera/front_6mm/image/compressed""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Color Camera"", ""name"": ""Telephoto Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 10, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/apollo/sensor/camera/front_12mm/image/compressed""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": -4, ""yaw"": 0, ""roll"": 0}}",
            @"{""type"": ""Keyboard Control"", ""name"": ""Keyboard Car Control""}",
            @"{""type"": ""Wheel Control"", ""name"": ""Wheel Car Control""}",
            @"{""type"": ""Vehicle Control"", ""name"": ""Apollo Car Control"",
            ""params"": {""Topic"": ""/apollo/control""} }"
        ) + "]";

        public static readonly string DataCollection = "[" + string.Join(",",
            @"{""type"": ""Lidar"", ""name"": ""Lidar"",
            ""params"": {""LaserCount"": 32, ""MinDistance"": 0.5, ""MaxDistance"": 100, ""RotationFrequency"": 10, ""MeasurementsPerRotation"": 360, ""FieldOfView"": 41.33, ""CenterAngle"": 10, ""Compensated"": true, ""PointColor"": ""#ff000000"", ""Topic"": ""/simulator/lidar"", ""Frame"": ""velodyne""},
            ""transform"": {""x"": 0, ""y"": 2.312, ""z"": -0.3679201, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",

            @"{""type"": ""Color Camera"", ""name"": ""Main Camera"",
            ""params"": {""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/simulator/camera/color/compressed""},
            ""transform"": {""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",

            @"{""type"": ""Depth Camera"", ""name"": ""Depth Camera"",
            ""params"": { ""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""JpegQuality"": 75, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/simulator/depth_camera/compressed""},
            ""transform"": { ""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0}}",

            @"{""type"": ""Segmentation Camera"", ""name"": ""Segmentation Camera"",
            ""params"": { ""Width"": 1920, ""Height"": 1080, ""Frequency"": 15, ""FieldOfView"": 50, ""MinDistance"": 0.1, ""MaxDistance"": 2000, ""Topic"": ""/simulator/segmentation_camera/compressed"" },
            ""transform"": { ""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0 }}",

            @"{""type"": ""3D Ground Truth"", ""name"": ""3D Ground Truth"",
            ""params"": { ""Frequency"": 10, ""Topic"": ""/simulator/ground_truth/3d_detections"" },
            ""transform"": { ""x"": 0, ""y"": 1.975314, ""z"": -0.3679201, ""pitch"": 0, ""yaw"": 0, ""roll"": 0 }}",

            @"{""type"": ""2D Ground Truth"", ""name"": ""2D Ground Truth"",
            ""params"": { ""Frequency"": 10, ""Topic"": ""/simulator/ground_truth/2d_detections"" },
            ""transform"": { ""x"": 0, ""y"": 1.7, ""z"": -0.2, ""pitch"": 0, ""yaw"": 0, ""roll"": 0 }}",

            @"{""type"": ""Keyboard Control"", ""name"": ""Keyboard Car Control""}",
            @"{""type"": ""Wheel Control"", ""name"": ""Wheel Car Control""}") + "]";
    }
}
