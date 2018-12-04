/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

public struct LidarTemplate
{
    public string Name;
    public int RayCount;
    public float MinDistance;
    public float MaxDistance;
    public float RotationFrequency;
    public int MeasurementsPerRotation;
    public float FieldOfView;
    public float CenterAngle;

    public static readonly LidarTemplate[] Templates =
    {
        new LidarTemplate()
        {
            Name = "Custom",
        },

        new LidarTemplate()
        {
            Name = "Lidar16",
            RayCount = 16,
            MinDistance = 0.5f,
            MaxDistance = 100.0f,
            RotationFrequency = 10, // 5 .. 20
            MeasurementsPerRotation = 1000, // 900 .. 3600
            FieldOfView = 30.0f,
            CenterAngle = 0.0f,
        },

        new LidarTemplate()
        {
            Name = "Lidar16b",
            RayCount = 16,
            MinDistance = 0.5f,
            MaxDistance = 100.0f,
            RotationFrequency = 10, // 5 .. 20
            MeasurementsPerRotation = 1500, // 900 .. 3600
            FieldOfView = 20.0f,
            CenterAngle = 0.0f,
        },

        new LidarTemplate()
        {
            Name = "Lidar32",
            RayCount = 32,
            MinDistance = 0.5f,
            MaxDistance = 100.0f,
            RotationFrequency = 10, // 5 .. 20
            MeasurementsPerRotation = 1500, // 900 .. 3600
            FieldOfView = 41.33f,
            CenterAngle = 10.0f,
        },

        //new LidarTemplate()
        //{
        //    Name = "Lidar32b",
        //    RayCount = 32,
        //    MinDistance = 0.5f,
        //    MaxDistance = 200.0f,
        //    RotationFrequency = 10, // 5 .. 20
        //    MeasurementsPerRotation = 2000, // 900 .. 3600
        //    FieldOfView = 40.0f,
        //    CenterAngle = 5.0f,
        //},

        new LidarTemplate()
        {
            Name = "Lidar64",
            RayCount = 64,
            MinDistance = 0.5f,
            MaxDistance = 120.0f,
            RotationFrequency = 5, // 5 .. 20
            MeasurementsPerRotation = 2000, // 1028 .. 4500
            FieldOfView = 26.9f,
            CenterAngle = 11.45f,
        },

        new LidarTemplate()
        {
            Name = "Lidar128",
            RayCount = 128,
            MinDistance = 0.5f,
            MaxDistance = 300.0f,
            RotationFrequency = 10,
            MeasurementsPerRotation = 3272,
            FieldOfView = 40.0f,
            CenterAngle = 5.0f,
        },
    };
}
