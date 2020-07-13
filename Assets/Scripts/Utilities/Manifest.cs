/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator
{
    public class Manifest
    {
        public string assetName;
        public string assetType;
        public string assetGuid;
        public int assetFormat;
        public double[] mapOrigin;
        public double[] baseLink;
        public string description;
        public string licenseName;
        public string authorName;
        public string authorUrl;
        public string fmuName;
        public string copyright;
        public Dictionary<string, object> attachments;
        public Dictionary<string, Simulator.Utilities.SensorParam> sensorParams;
        public string[] bridgeDataTypes;
    }

    public struct Images
    {
        public string small;
        public string medium;
        public string large;
    }

    public struct HdMaps
    {
        public string apollo30;
        public string apollo50;
        public string autoware;
        public string lanelet2;
        public string opendrive;
    }
}
