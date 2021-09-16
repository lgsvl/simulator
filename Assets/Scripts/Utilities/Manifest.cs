/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Utilities;
using System.Collections.Generic;

namespace Simulator
{
    public class Manifest
    {
        public string assetName;
        public string assetType;
        public string assetGuid;
        public string assetFormat;
        public double[] mapOrigin;
        public double[] baseLink;
        public string description;
        public string fmuName;
        public Dictionary<string, object> attachments;
        public List<Simulator.Utilities.SensorParam> sensorParams;
        public string[] bridgeDataTypes;
        public string[] supportedBridgeTypes;
        public string bridgeType;
    }

    public struct HdMaps
    {
        public string apollo50;
        public string autoware;
        public string lanelet2;
        public string opendrive;
    }
}
