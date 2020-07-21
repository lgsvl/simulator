/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class SignalData
    {
        public uint SeqId;
        public string Id;
        public string Label;
        public double Score;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public class SignalDataArray
    {
        public double Time;
        public string Frame;
        public uint Sequence;
        public SignalData[] Data;
    }
}
