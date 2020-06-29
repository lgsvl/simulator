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
        public string assetGuid;
        public int bundleFormat;
        public string description;
        public string licenseName;
        public string authorName;
        public string authorUrl;
        public string fmuName;
        public double[] mapOrigin;
        public double[] baseLink;

        public Dictionary<string, string> additionalFiles;
        public string[] bridgeDataTypes;
    }
}
