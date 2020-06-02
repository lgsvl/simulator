/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator
{
    public struct Manifest
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
        public Dictionary<string, Param> sensorParams;
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

    public struct Param
    {
        public string Type;
        public object DefaultValue;
        public string[] Values;
        public float? Min;
        public float? Max;
        public string Unit;
    }
}
