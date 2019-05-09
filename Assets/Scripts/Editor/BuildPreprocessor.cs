/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Simulator.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            var info = new Simulator.Utilities.BuildInfo()
            {
                Timestamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                Version = Environment.GetEnvironmentVariable("BUILD_VERSION"),
                GitCommitId = Environment.GetEnvironmentVariable("GIT_COMMIT"),
                GitBranchName = Environment.GetEnvironmentVariable("GIT_BRANCH_NAME"),
            };

            AssetDatabase.CreateAsset(info, "Assets/Resources/BuildInfo.asset");
        }
    }
}
