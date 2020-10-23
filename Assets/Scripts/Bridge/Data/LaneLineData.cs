/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data
{
    using UnityEngine;

    public enum LaneLineType
    {
        WHITE_DASHED = 0,
        WHITE_SOLID = 1,
        YELLOW_DASHED = 2,
        YELLOW_SOLID = 3,
    }

    public struct LaneLineCubicCurve
    {
        public float MinX, MaxX;
        public float C0, C1, C2, C3;

        public LaneLineCubicCurve(float min, float max, Vector4 coefficients)
        {
            MinX = min;
            MaxX = max;
            C0 = coefficients[0];
            C1 = coefficients[1];
            C2 = coefficients[2];
            C3 = coefficients[3];
        }

        public float Sample(float x)
        {
            return C0 + x * C1 + x * x * C2 + x * x * x * C3;
        }
    }

    public class LaneLineData
    {
        public uint Sequence;
        public string Frame;
        public double Time;
        public float LineWidth = 0;
        public LaneLineType Type;
        public LaneLineCubicCurve CurveCameraCoord;
        public LaneLineCubicCurve CurveImageCoord;
    }
}
