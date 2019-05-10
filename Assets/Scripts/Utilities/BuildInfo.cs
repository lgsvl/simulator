/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;

namespace Simulator.Utilities
{
    public class BuildInfo : ScriptableObject
    {
        public string Timestamp;
        public string Version;
        public string GitCommit;
        public string GitBranch;
    }
}
