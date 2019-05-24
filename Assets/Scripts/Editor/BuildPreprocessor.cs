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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simulator.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly string BuildInfoAsset = "Assets/Resources/BuildInfo.asset";

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            var info = ScriptableObject.CreateInstance<Utilities.BuildInfo>();

            info.Timestamp = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);
            info.Version = Environment.GetEnvironmentVariable("BUILD_VERSION");
            info.GitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
            info.GitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");

            info.DownloadHost = Environment.GetEnvironmentVariable("S3_DOWNLOAD_HOST");

            if (info.GitBranch != null && info.GitBranch == "master")
            {
                var external = Path.Combine(Application.dataPath, "External");

                var environments = new Dictionary<string, bool?>();
                Build.Refresh(environments, Path.Combine(external, "Environments"), Build.SceneExtension);
                info.DownloadEnvironments = environments.Where(kv => kv.Value.GetValueOrDefault()).Select(kv => kv.Key).OrderBy(e => e).ToArray();

                var vehicles = new Dictionary<string, bool?>();
                Build.Refresh(vehicles, Path.Combine(external, "Vehicles"), Build.PrefabExtension);
                info.DownloadVehicles = vehicles.Where(kv => kv.Value.GetValueOrDefault()).Select(kv => kv.Key).OrderBy(e => e).ToArray();
            }

            AssetDatabase.CreateAsset(info, BuildInfoAsset);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            AssetDatabase.DeleteAsset(BuildInfoAsset);
        }
    }
}
