/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Simulator.Editor
{
    public static class BuildWebUI
    {
        [MenuItem("Simulator/Build WebUI...", false, 40)]
        public static void Build()
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = Path.Combine(Application.dataPath, "..", "WebUI"),
                    FileName = "npm",
                    Arguments = "run pack",
                };

                try
                {
                    if (!process.Start())
                    {
                        throw new Exception("Could not start npm process");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    UnityEngine.Debug.LogError(
                        "Failed to build WebUI. Please make sure <b>npm</b> is installed. Check <color=red>README.md</color> file");
                    return;
                }

                var title = "Building WebUI...";
                var info = string.Empty;

                float progress = 0.0f;

                while (!process.HasExited)
                {
                    progress = (progress + Time.deltaTime / 10.0f) % 1.0f;
                    EditorUtility.DisplayProgressBar(title, info, progress);

                    process.WaitForExit(100);
                }

                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log($"WebUI build is completed");
                }
                else
                {
                    UnityEngine.Debug.LogError($"Error building WebUI");
                }

                EditorUtility.ClearProgressBar();
            }
        }
    }
}
