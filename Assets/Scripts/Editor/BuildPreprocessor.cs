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
using System.Linq;

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

            var simEnvironments = Environment.GetEnvironmentVariable("SIM_ENVIRONMENTS");
            if (!string.IsNullOrEmpty(simEnvironments))
            {
                info.DownloadEnvironments = simEnvironments.Split('\n').Select(line =>
                {
                    var items = line.Split(new[] { ' ' }, 2);
                    return new Utilities.BuildItem()
                    {
                        Id = items[0],
                        Name = items[1],
                    };
                }).ToArray();
            }

            var simVehicles = Environment.GetEnvironmentVariable("SIM_VEHICLES");
            if (!string.IsNullOrEmpty(simVehicles))
            {
                info.DownloadVehicles = simVehicles.Split('\n').Select(line =>
                {
                    var items = line.Split(new[] { ' ' }, 2);
                    return new Utilities.BuildItem()
                    {
                        Id = items[0],
                        Name = items[1],
                    };
                }).ToArray();
            }

            AssetDatabase.CreateAsset(info, BuildInfoAsset);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            AssetDatabase.DeleteAsset(BuildInfoAsset);
        }
    }
}
