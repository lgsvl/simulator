using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class TrafficLightData
    {
        public double Time;
        public uint Sequence;
        public bool blink;
        public double confidence;
        public string color;
    }
}