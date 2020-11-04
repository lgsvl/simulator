/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data
{
    using System.Collections.Generic;
    using UnityEngine;

    public enum LaneLineType
    {
        WhiteDashed = 0,
        WhiteSolid = 1,
        YellowDashed = 2,
        YellowSolid = 3,
    }
    
    public enum LaneLinePositionType
    {
        BollardLeft = -5,
        FourthLeft = -4,
        ThirdLeft = -3,
        AdjacentLeft = -2,
        EgoLeft = -1,
        EgoRight = 1,
        AdjacentRight = 2,
        ThirdRight = 3,
        FourthRight = 4,
        BollardRight = 5,
        Other = 6,
        Unknown = 7
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

    public class LaneLinesData
    {
        public uint Sequence;
        public string Frame;
        public double Time;
        public List<LaneLineData> lineData;
    }
    
    public class LaneLineData
    {
        public float LineWidth = 0;
        public LaneLineType Type;
        public LaneLinePositionType PositionType;
        public LaneLineCubicCurve CurveCameraCoord;
    }
}
