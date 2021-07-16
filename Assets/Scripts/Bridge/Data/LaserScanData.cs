/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data
{
    using System;
    using UnityEngine;

    public class LaserScanData : IThreadCachedBridgeData<LaserScanData>
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public float MinAngle;
        public float MaxAngle;
        public float AngleStep;

        public float RangeMin;
        public float RangeMax;

        public float TimeIncrement;
        public float ScanTime;

        public Matrix4x4 Transform;

        public Vector4[] Points;

        // These two values are caches for bridge converters to avoid reallocations
        public float[] RangesCache;
        public float[] IntensitiesCache;

        public void CopyToCache(LaserScanData target)
        {
            target.Name = Name;
            target.Frame = Frame;
            target.Time = Time;
            target.Sequence = Sequence;
            target.MinAngle = MinAngle;
            target.MaxAngle = MaxAngle;
            target.AngleStep = AngleStep;
            target.RangeMin = RangeMin;
            target.RangeMax = RangeMax;
            target.TimeIncrement = TimeIncrement;
            target.ScanTime = ScanTime;
            target.Transform = Transform;

            if (target.Points == null || target.Points.Length != Points.Length)
                target.Points = new Vector4[Points.Length];

            Array.Copy(Points, target.Points, Points.Length);
        }

        public PointCloudData ConvertToPointCloudData()
        {
            return new PointCloudData()
            {
                Name = Name,
                Frame = Frame,
                Time = Time,
                Sequence = Sequence,

                LaserCount = 1,
                Transform = Transform,
                Points = Points,
                PointCount = Points.Length
            };
        }

        public int GetHash()
        {
            return Points.Length;
        }
    }
}