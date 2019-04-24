using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;

namespace Simulator.Editor
{
    public static class BuildWebUI
    {
        [MenuItem("Simulator/Build WebUI")]
        public static void Build()
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = Path.Combine(Application.dataPath, "..", "WebUI"),
                    FileName = "npm",
                    Arguments = "run pack-p",
                };

                if (!process.Start())
                {
                    UnityEngine.Debug.LogError("Failed to build WebUI. Please make sure <b>npm</b> is installed. Check <color=red>README.md</color> file");
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
