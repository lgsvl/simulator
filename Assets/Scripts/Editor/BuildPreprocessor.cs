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
            var info = ScriptableObject.CreateInstance<Utilities.BuildInfo>();

            info.Timestamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            info.Version = Environment.GetEnvironmentVariable("BUILD_VERSION");
            info.GitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
            info.GitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");

            AssetDatabase.CreateAsset(info, "Assets/Resources/BuildInfo.asset");
        }
    }
}
