/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Utilities
{
    public class BuildInfo : ScriptableObject
    {
        public string Timestamp;
        public string Version;
        public string GitCommit;
        public string GitBranch;

        public string DownloadHost;
        public string[] DownloadEnvironments;
        public string[] DownloadVehicles;
    }
}
