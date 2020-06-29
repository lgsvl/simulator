/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Sensors
{
    public partial class LidarSensor
    {
        public struct Template
        {
            public string Name;
            public int LaserCount;
            public float MinDistance;
            public float MaxDistance;
            public float RotationFrequency;
            public int MeasurementsPerRotation;
            public float FieldOfView;
            public List<float> VerticalRayAngles;
            public float CenterAngle;

            public static readonly Template[] Templates =
            {
                new Template()
                {
                    Name = "Custom",
                },

                new Template()
                {
                    Name = "Lidar16",
                    LaserCount = 16,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1000, // 900 .. 3600
                    FieldOfView = 30.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 0.0f,
                },

                new Template()
                {
                    Name = "Lidar16b",
                    LaserCount = 16,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 20.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 0.0f,
                },

                new Template()
                {
                    Name = "Lidar32",
                    LaserCount = 32,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 41.33f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 10.0f,
                },

                new Template()
                {
                    Name = "Lidar32-NonUniform",
                    LaserCount = 32,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 41.33f,
                    VerticalRayAngles = new List<float> {
                        -25.0f,   -1.0f,    -1.667f,  -15.639f,
                        -11.31f,   0.0f,    -0.667f,   -8.843f,
                         -7.254f,  0.333f,  -0.333f,   -6.148f,
                         -5.333f,  1.333f,   0.667f,   -4.0f,
                         -4.667f,  1.667f,   1.0f,     -3.667f,
                         -3.333f,  3.333f,   2.333f,   -2.667f,
                         -3.0f,    7.0f,     4.667f,   -2.333f,
                         -2.0f,   15.0f,    10.333f,   -1.333f
                        },
                    CenterAngle = 10.0f,
                },

                new Template()
                {
                    Name = "Lidar64",
                    LaserCount = 64,
                    MinDistance = 0.5f,
                    MaxDistance = 120.0f,
                    RotationFrequency = 5, // 5 .. 20
                    MeasurementsPerRotation = 2000, // 1028 .. 4500
                    FieldOfView = 26.9f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 11.45f,
                },

                new Template()
                {
                    Name = "Lidar128",
                    LaserCount = 128,
                    MinDistance = 0.5f,
                    MaxDistance = 300.0f,
                    RotationFrequency = 10,
                    MeasurementsPerRotation = 3272,
                    FieldOfView = 40.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 5.0f,
                },
            };
        }
    }
}
