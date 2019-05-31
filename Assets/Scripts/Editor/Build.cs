/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Simulator.Editor
{
    public class Build : EditorWindow
    {
        enum BuildTarget
        {
            Windows,
            Linux,
            MacOS,
        }

        public const string SceneExtension = "unity";
        public const string PrefabExtension = "prefab";

        Vector2 EnvironmentScroll;
        Vector2 VehicleScroll;

        Dictionary<string, bool?> Environments = new Dictionary<string, bool?>();
        Dictionary<string, bool?> Vehicles = new Dictionary<string, bool?>();

        [SerializeField] BuildTarget Target;
        [SerializeField] bool BuildPlayer = true;
        [SerializeField] string PlayerFolder = string.Empty;
        [SerializeField] bool DevelopmentPlayer = false;

        [MenuItem("Simulator/Build...", false, 30)]
        static void ShowWindow()
        {
            var window = GetWindow<Build>();
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                window.Target = BuildTarget.Windows;
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                window.Target = BuildTarget.Linux;
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                window.Target = BuildTarget.MacOS;
            }

            var data = EditorPrefs.GetString("Simulator/Build", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);

            window.Show();
        }

        void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Simulator/Build", data);
        }

        void OnGUI()
        {
            GUILayout.Label("Environments", EditorStyles.boldLabel);

            EnvironmentScroll = EditorGUILayout.BeginScrollView(EnvironmentScroll);
            foreach (var name in Environments.Keys.OrderBy(name => name))
            {
                var check = Environments[name];
                if (check.HasValue)
                {
                    Environments[name] = GUILayout.Toggle(check.Value, name);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Toggle(false, $"{name} (missing Environments/{name}/{name}.scene file)");
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Label("Vehicles", EditorStyles.boldLabel);
            VehicleScroll = EditorGUILayout.BeginScrollView(VehicleScroll);
            foreach (var name in Vehicles.Keys.OrderBy(name => name))
            {
                var check = Vehicles[name];
                if (check.HasValue)
                {
                    Vehicles[name] = GUILayout.Toggle(check.Value, name);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Toggle(false, $"{name} (missing Vehicles/{name}/{name}.prefab file)");
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Label("Options", EditorStyles.boldLabel);

            Target = (BuildTarget)EditorGUILayout.EnumPopup("Target OS:", Target);

            var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            BuildPlayer = GUILayout.Toggle(BuildPlayer, "Build Simulator:", GUILayout.ExpandWidth(false));

            EditorGUI.BeginDisabledGroup(!BuildPlayer);
            PlayerFolder = GUILayout.TextField(PlayerFolder);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var folder = EditorUtility.SaveFolderPanel("Choose folder", PlayerFolder, string.Empty);
                if (!string.IsNullOrEmpty(folder))
                {
                    PlayerFolder = Path.GetFullPath(folder);
                }
            }
            EditorGUILayout.EndHorizontal();

            DevelopmentPlayer = GUILayout.Toggle(DevelopmentPlayer, "Development Build");
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Build", GUILayout.ExpandWidth(false)))
            {
                var assetBundlesLocation = Path.Combine(Application.dataPath, "..", "AssetBundles");
                if (BuildPlayer)
                {
                    if (string.IsNullOrEmpty(PlayerFolder))
                    {
                        Debug.LogError("Please specify simulator build folder!");
                        return;
                    }
                    RunPlayerBuild(Target, PlayerFolder, DevelopmentPlayer);

                    assetBundlesLocation = Path.Combine(PlayerFolder, "AssetBundles");
                }

                var environments = Environments.Where(kv => kv.Value.HasValue && kv.Value.Value).Select(kv => kv.Key);
                var vehicles = Vehicles.Where(kv => kv.Value.HasValue && kv.Value.Value).Select(kv => kv.Key);

                RunAssetBundleBuild(Target, assetBundlesLocation, null, environments.ToList(), vehicles.ToList());
            }
        }

        void OnFocus()
        {
            var external = Path.Combine(Application.dataPath, "External");

            Refresh(Environments, Path.Combine(external, "Environments"), SceneExtension);
            Refresh(Vehicles, Path.Combine(external, "Vehicles"), PrefabExtension);
        }

        public static void Refresh(Dictionary<string, bool?> items, string folder, string suffix)
        {
            var updated = new HashSet<string>();

            foreach (var path in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(path);

                if ((File.GetAttributes(path) & FileAttributes.Directory) == 0)
                {
                    continue;
                }

                if (!items.ContainsKey(name))
                {
                    var fullPath = Path.Combine(path, $"{name}.{suffix}");
                    bool? check = null;
                    if (File.Exists(fullPath))
                    {
                        check = true;
                    }
                    items.Add(name, check);
                }

                updated.Add(name);
            }

            var removed = items.Where(kv => !updated.Contains(kv.Key)).Select(kv => kv.Key).ToArray();
            Array.ForEach(removed, remove => items.Remove(remove));
        }

        static void RunAssetBundleBuild(BuildTarget target, string folder, string saveLinks, List<string> environments, List<string> vehicles)
        {
            UnityEditor.BuildTarget buildTarget;
            if (target == BuildTarget.Windows)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneWindows64;
            }
            else if (target == BuildTarget.Linux)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneLinux64;
            }
            else if (target == BuildTarget.MacOS)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneOSX;
            }
            else
            {
                throw new Exception($"Unsupported build target {target}");
            }

            var builds = new List<AssetBundleBuild>();
            foreach (var name in environments)
            {
                var asset = Path.Combine("Assets", "External", "Environments", name, $"{name}.unity");
                builds.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"environment_{name}",
                    assetNames = new[] { asset },
                });
            }

            foreach (var name in vehicles)
            {
                var asset = Path.Combine("Assets", "External", "Vehicles", name, $"{name}.prefab");
                builds.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"vehicle_{name}",
                    assetNames = new[] { asset },
                });
            }

            if (builds.Count == 0)
            {
                Debug.LogWarning("No asset bundles selected!");
                return;
            }

            Directory.CreateDirectory(folder);
            var manifest = BuildPipeline.BuildAssetBundles(
                folder,
                builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                buildTarget);

            if (manifest == null || manifest.GetAllAssetBundles().Length != builds.Count)
            {
                Debug.LogError($"Failed to build some of asset bundles!");
            }
            else
            {
                Debug.Log($"All asset bundles successfully built!");

                if (saveLinks != null)
                {
                    SaveBundleLinks(saveLinks, target, folder, environments, vehicles);
                }
            }
        }

        static void SaveBundleLinks(string filename, BuildTarget target, string bundleFolder, List<string> environments, List<string> vehicles)
        {
            var gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
            var gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
            var downloadHost = Environment.GetEnvironmentVariable("S3_DOWNLOAD_HOST");

            if (string.IsNullOrEmpty(gitCommit))
            {
                Debug.LogError("Cannot save bundle links - GIT_COMMIT is not set");
                return;
            }

            if (string.IsNullOrEmpty(gitBranch) || gitBranch != "master")
            {
                Debug.LogError("Cannot save bundle links - GIT_BRANCH is not set or not 'master'");
                return;
            }

            if (string.IsNullOrEmpty(downloadHost))
            {
                Debug.LogError("Cannot save bundle links - S3_DOWNLOAD_HOST is not set");
                return;
            }

            string os;
            if (target == BuildTarget.Windows)
            {
                os = "windows";
            }
            else if (target == BuildTarget.Linux)
            {
                os = "linux";
            }
            else if (target == BuildTarget.MacOS)
            {
                os = "macos";
            }
            else
            {
                Debug.LogError("Cannot save bundle links - unknown build target");
                return;
            }

            using (var f = File.CreateText(filename))
            {
                f.WriteLine("<html><body>");

                var dt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

                f.WriteLine("<h1>Info</h1>");
                f.WriteLine("<ul>");
                f.WriteLine($"<li>Build Date: {dt}</li>");
                f.WriteLine($"<li>Git Commit: {gitCommit}</li>");
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Environments</h1>");
                f.WriteLine("<ul>");
                foreach (var name in environments)
                {
                    var url = $"https://{downloadHost}/{gitCommit}/{os}/environment_{name.ToLowerInvariant()}";
                    var bundle = Path.Combine(bundleFolder, $"environment_{name.ToLowerInvariant()}");
                    var size = EditorUtility.FormatBytes(new FileInfo(bundle).Length);
                    f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                }
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Vehicles</h1>");
                f.WriteLine("<ul>");
                foreach (var name in vehicles)
                {
                    var url = $"https://{downloadHost}/{gitCommit}/{os}/vehicle_{name.ToLowerInvariant()}";
                    var bundle = Path.Combine(bundleFolder, $"vehicle_{name.ToLowerInvariant()}");
                    var size = EditorUtility.FormatBytes(new FileInfo(bundle).Length);
                    f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                }
                f.WriteLine("</ul>");

                f.WriteLine("</body></html>");
            }
        }

        static void RunPlayerBuild(BuildTarget target, string folder, bool development)
        {
            // TODO: this is temporary until we learn how to build WebUI output directly in Web folder
            var webui = Path.Combine(Application.dataPath, "..", "WebUI", "dist");
            if (!File.Exists(Path.Combine(webui, "index.html")))
            {
                throw new Exception($"WebUI files are missing! Please build WebUI at least once before building Player");
            }

            UnityEditor.BuildTarget buildTarget;
            if (target == BuildTarget.Windows)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneWindows64;
            }
            else if (target == BuildTarget.Linux)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneLinux64;
            }
            else if (target == BuildTarget.MacOS)
            {
                buildTarget = UnityEditor.BuildTarget.StandaloneOSX;
            }
            else
            {
                throw new Exception($"Unsupported build target {target}");
            }

            string location;
            if (target == BuildTarget.Linux)
            {
                location = Path.Combine(folder, "simulator");
            }
            else if (target == BuildTarget.Windows)
            {
                location = Path.Combine(folder, "simulator.exe");
            }
            else if (target == BuildTarget.MacOS)
            {
                location = Path.Combine(folder, "simulator");
            }
            else
            {
                Debug.LogError($"Target {target} is not supported");
                return;
            }

            var build = new BuildPlayerOptions()
            {
                scenes = new[] { "Assets/Scenes/LoaderScene.unity" },
                locationPathName = location,
                targetGroup = BuildTargetGroup.Standalone,
                target = buildTarget,
                options = BuildOptions.CompressWithLz4 | BuildOptions.StrictMode,
            };

            if (development)
            {
                build.options |= BuildOptions.Development;
            }

            var r = BuildPipeline.BuildPlayer(build);
            if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                // TODO: this is temporary until we learn how to build WebUI output directly in Web folder
                var webFolder = target == BuildTarget.MacOS ? Path.Combine(folder, "simulator.app") : folder;
                var web = Path.Combine(webFolder, "Web");
                Directory.CreateDirectory(web);

                var files = new[] { "index.html", "main.css", "main.js" };
                foreach (var file in files)
                {
                    File.Copy(Path.Combine(webui, file), Path.Combine(web, file), true);
                }

                Debug.Log("Player build succeeded!");
            }
            else
            {
                Debug.LogError($"Player build result: {r.summary.result}!");
            }
        }

        static void Run()
        {
            List<string> environments = null;
            List<string> vehicles = null;
            BuildTarget? buildTarget = null;
            string buildOutput = null;
            string saveBundleLinks = null;

            bool skipPlayer = false;
            bool skipBundles = false;
            bool developmentBuild = false;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildTarget")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        if (args[i] == "Win64")
                        {
                            buildTarget = BuildTarget.Windows;
                        }
                        else if (args[i] == "Linux64")
                        {
                            buildTarget = BuildTarget.Linux;
                        }
                        else if (args[i] == "OSXUniversal")
                        {
                            buildTarget = BuildTarget.MacOS;
                        }
                        else
                        {
                            throw new Exception($"Unsupported '{args[i]}' build target!");
                        }
                    }
                    else
                    {
                        throw new Exception("-buildTarget expects Win64 or Linux64 argument!");
                    }
                }
                else if (args[i] == "-buildOutput")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        buildOutput = args[i];
                    }
                    else
                    {
                        throw new Exception("-buildOutput expects output folder!");
                    }
                }
                else if (args[i] == "-saveBundleLinks")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        saveBundleLinks = args[i];
                    }
                    else
                    {
                        throw new Exception("-saveBundleLinks expects output filename!");
                    }
                }
                else if (args[i] == "-buildEnvironments")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        if (args[i] != "all")
                        {
                            environments = args[i].Split(',').ToList();
                        }
                    }
                    else
                    {
                        throw new Exception("-buildEnvironments expects comma seperated environment names!");
                    }
                }
                else if (args[i] == "-buildVehicles")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        if (args[i] != "all")
                        {
                            vehicles = args[i].Split(',').ToList();
                        }
                    }
                    else
                    {
                        throw new Exception("-buildVehicles expects comma seperated vehicle names!");
                    }
                }
                else if (args[i] == "-skipPlayer")
                {
                    skipPlayer = true;
                }
                else if (args[i] == "-skipBundles")
                {
                    skipBundles = true;
                }
                else if (args[i] == "-developmentBuild")
                {
                    developmentBuild = true;
                }
            }

            if (!buildTarget.HasValue)
            {
                throw new Exception("-buildTarget not specified!");
            }

            if (string.IsNullOrEmpty(buildOutput))
            {
                throw new Exception("-buildOutput not specified!");
            }

            var external = Path.Combine(Application.dataPath, "External");

            if (!skipBundles)
            {
                var availEnvironments = new Dictionary<string, bool?>();
                var availVehicles = new Dictionary<string, bool?>();

                Refresh(availEnvironments, Path.Combine(external, "Environments"), SceneExtension);
                Refresh(availVehicles, Path.Combine(external, "Vehicles"), PrefabExtension);

                if (environments == null)
                {
                    environments = availEnvironments.Where(kv => kv.Value.HasValue).Select(kv => kv.Key).ToList();
                }
                else
                {
                    foreach (var environment in environments)
                    {
                        if (!availEnvironments.ContainsKey(environment))
                        {
                            throw new Exception($"Environment '{environment}' is not available");
                        }
                    }
                }

                if (vehicles == null)
                {
                    vehicles = availVehicles.Where(kv => kv.Value.HasValue).Select(kv => kv.Key).ToList();
                }
                else
                {
                    foreach (var vehicle in vehicles)
                    {
                        if (!availVehicles.ContainsKey(vehicle))
                        {
                            throw new Exception($"Vehicle '{vehicle}' is not available");
                        }
                    }
                }
            }

            var assetBundlesLocation = buildOutput;

            if (!skipPlayer)
            {
                RunPlayerBuild(buildTarget.Value, buildOutput, developmentBuild);

                assetBundlesLocation = Path.Combine(buildOutput, "AssetBundles");
            }

            if (!skipBundles)
            {
                RunAssetBundleBuild(buildTarget.Value, assetBundlesLocation, saveBundleLinks, environments, vehicles);
            }
        }
    }
}
