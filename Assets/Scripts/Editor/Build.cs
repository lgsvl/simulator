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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Map;
using YamlDotNet.Serialization;
using System.Net;
using UnityEditor.Compilation;
using System.Reflection;
using System.Text;

namespace Simulator.Editor
{
    public class Build : EditorWindow
    {
        static public bool Running;

        enum BuildTarget
        {
            Windows,
            Linux,
            MacOS,
        }

        class AsmdefBody
        {
            public string name;
            public string[] references;
        }

        public const string SceneExtension = "unity";
        public const string PrefabExtension = "prefab";

        Vector2 EnvironmentScroll;
        Vector2 VehicleScroll;

        Dictionary<string, bool?> Environments = new Dictionary<string, bool?>();
        Dictionary<string, bool?> Vehicles = new Dictionary<string, bool?>();

        [SerializeField] BuildTarget Target;
        [SerializeField] bool BuildPlayer = false;
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
            window.titleContent = new GUIContent("Build Maps & Vehicles");

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
            EditorGUILayout.HelpBox("Following environment were automatically detected:", UnityEditor.MessageType.None);

            EnvironmentScroll = EditorGUILayout.BeginScrollView(EnvironmentScroll);

            if (Environments.Keys.Count != 0)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                {
                    foreach (var key in Environments.Keys.ToArray())
                    {
                        Environments[key] = true;
                    }
                }
                if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                {
                    foreach (var key in Environments.Keys.ToArray())
                    {
                        Environments[key] = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

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
                    GUILayout.Toggle(false, $"{name} (missing Environments/{name}/{name}.{SceneExtension} file)");
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Label("Vehicles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Following vehicles were automatically detected:", UnityEditor.MessageType.None);

            VehicleScroll = EditorGUILayout.BeginScrollView(VehicleScroll);

            if (Vehicles.Keys.Count != 0)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                {
                    foreach (var key in Vehicles.Keys.ToArray())
                    {
                        Vehicles[key] = true;
                    }
                }
                if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                {
                    foreach (var key in Vehicles.Keys.ToArray())
                    {
                        Vehicles[key] = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

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
                    GUILayout.Toggle(false, $"{name} (missing Vehicles/{name}/{name}.{PrefabExtension} file)");
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Label("Options", EditorStyles.boldLabel);

            Target = (BuildTarget)EditorGUILayout.EnumPopup("Executable Platform:", Target);

            EditorGUILayout.HelpBox("Select Folder to Save...", MessageType.Info);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            BuildPlayer = GUILayout.Toggle(BuildPlayer, "Build Simulator:", GUILayout.ExpandWidth(false));

            EditorGUI.BeginDisabledGroup(!BuildPlayer);
            PlayerFolder = GUILayout.TextField(PlayerFolder);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var folder = EditorUtility.SaveFolderPanel("Select Folder", PlayerFolder, string.Empty);
                if (!string.IsNullOrEmpty(folder))
                {
                    PlayerFolder = Path.GetFullPath(folder);
                }
            }
            EditorGUILayout.EndHorizontal();

            DevelopmentPlayer = GUILayout.Toggle(DevelopmentPlayer, "Development Build");
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Build"))
            {
                Running = true;
                try
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

                    RunAssetBundleBuild(assetBundlesLocation, environments.ToList(), vehicles.ToList());
                }
                finally
                {
                    Running = false;
                }
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
                        check = false;
                    }
                    items.Add(name, check);
                }

                updated.Add(name);
            }

            var removed = items.Where(kv => !updated.Contains(kv.Key)).Select(kv => kv.Key).ToArray();
            Array.ForEach(removed, remove => items.Remove(remove));
        }

        static void RunAssetBundleBuild(string folder, List<string> environments, List<string> vehicles)
        {
            Directory.CreateDirectory(folder);

            var envManifests = new List<Manifest>();
            var vehicleManifests = new List<Manifest>();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Cancelling the build.");
                return;
            }

            var currentScenes = new HashSet<Scene>();
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                currentScenes.Add(EditorSceneManager.GetSceneAt(i));
            }

            foreach (var name in environments)
            {
                var scene = Path.Combine("Assets", "External", "Environments", name, $"{name}.{SceneExtension}");

                Scene s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
                try
                {
                    Manifest? manifest = null;

                    foreach (GameObject root in s.GetRootGameObjects())
                    {
                        MapOrigin origin = root.GetComponentInChildren<MapOrigin>();
                        if (origin != null)
                        {
                            manifest = new Manifest
                            {
                                assetName = name,
                                bundleGuid = Guid.NewGuid().ToString(),
                                bundleFormat = BundleConfig.BundleFormatVersion,
                                description = origin.Description,
                                licenseName = origin.LicenseName,
                                authorName = "",
                                authorUrl = "",
                            };
                            break;
                        }
                    }

                    if (manifest.HasValue)
                    {
                        envManifests.Add(manifest.Value);
                    }
                    else
                    {
                        throw new Exception($"Build failed: MapOrigin on {name} not found. Please add a MapOrigin component.");
                    }
                }
                finally
                {
                    if (!currentScenes.Contains(s))
                    {
                        EditorSceneManager.CloseScene(s, true);
                    }
                }
            }

            foreach (var name in vehicles)
            {
                var prefab = Path.Combine("Assets", "External", "Vehicles", name, $"{name}.{PrefabExtension}");
                VehicleInfo info = AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponent<VehicleInfo>();
                if (info == null)
                {
                    throw new Exception($"Build failed: Vehicle info on {name} not found. Please add a VehicleInfo component and rebuild.");
                }

                var manifest = new Manifest
                {
                    assetName = name,
                    bundleGuid = Guid.NewGuid().ToString(),
                    bundleFormat = BundleConfig.BundleFormatVersion,
                    description = info.Description,
                    licenseName = info.LicenseName,
                    authorName = "",
                    authorUrl = "",
                };

                vehicleManifests.Add(manifest);
            }

            foreach (var manifest in envManifests)
            {
                try
                {
                    var sceneAsset = Path.Combine("Assets", "External", "Environments", manifest.assetName, $"{manifest.assetName}.{SceneExtension}");

                    var textureBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_environment_textures",
                        assetNames = AssetDatabase.GetDependencies(sceneAsset).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg")).ToArray(),
                    };

                    var windowsBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_environment_main_windows",
                        assetNames = new[] { sceneAsset },
                    };

                    BuildPipeline.BuildAssetBundles(
                         folder,
                         new[] { textureBuild, windowsBuild },
                         BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                         UnityEditor.BuildTarget.StandaloneWindows64);

                    var linuxBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_environment_main_linux",
                        assetNames = new[] { sceneAsset },
                    };

                    BuildPipeline.BuildAssetBundles(
                         folder,
                         new[] { textureBuild, linuxBuild },
                         BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                         UnityEditor.BuildTarget.StandaloneLinux64);

                    File.WriteAllText(Path.Combine(folder, "manifest"), new Serializer().Serialize(manifest));
                    try
                    {
                        using (ZipFile archive = ZipFile.Create(Path.Combine(folder, $"environment_{manifest.assetName}")))
                        {
                            archive.BeginUpdate();
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, textureBuild.assetBundleName)), textureBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, linuxBuild.assetBundleName)), linuxBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, windowsBuild.assetBundleName)), windowsBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, "manifest")), "manifest", CompressionMethod.Stored, true);
                            archive.CommitUpdate();
                            archive.Close();
                        }
                    }
                    finally
                    {
                        File.Delete(Path.Combine(folder, "manifest"));
                    }
                }
                finally
                {
                    var di = new DirectoryInfo(folder);

                    var files = di.GetFiles($"{manifest.bundleGuid}*");
                    Array.ForEach(files, f => f.Delete());

                    files = di.GetFiles($"AssetBundles*");
                    Array.ForEach(files, f => f.Delete());
                }
            }

            foreach (var manifest in vehicleManifests)
            {
                try
                {
                    var prefabAsset = Path.Combine("Assets", "External", "Vehicles", manifest.assetName, $"{manifest.assetName}.{PrefabExtension}");

                    var textureBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_vehicle_textures",
                        assetNames = AssetDatabase.GetDependencies(prefabAsset).Where(a => a.EndsWith(".png") || a.EndsWith(".jpg")).ToArray()
                    };

                    var windowsBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_vehicle_main_windows",
                        assetNames = new[] { prefabAsset },
                    };

                    BuildPipeline.BuildAssetBundles(
                         folder,
                         new[] { textureBuild, windowsBuild },
                         BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                         UnityEditor.BuildTarget.StandaloneWindows64);

                    var linuxBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{manifest.bundleGuid}_vehicle_main_linux",
                        assetNames = new[] { prefabAsset },
                    };

                    BuildPipeline.BuildAssetBundles(
                         folder,
                         new[] { textureBuild, linuxBuild },
                         BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                         UnityEditor.BuildTarget.StandaloneLinux64);

                    File.WriteAllText(Path.Combine(folder, "manifest"), new Serializer().Serialize(manifest));
                    try
                    {
                        using (ZipFile archive = ZipFile.Create(Path.Combine(folder, $"vehicle_{manifest.assetName}")))
                        {
                            archive.BeginUpdate();
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, textureBuild.assetBundleName)), textureBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, linuxBuild.assetBundleName)), linuxBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, windowsBuild.assetBundleName)), windowsBuild.assetBundleName, CompressionMethod.Stored, true);
                            archive.Add(new StaticDiskDataSource(Path.Combine(folder, "manifest")), "manifest", CompressionMethod.Stored, true);
                            archive.CommitUpdate();
                            archive.Close();
                        }
                    }
                    finally
                    {
                        File.Delete(Path.Combine(folder, "manifest"));
                    }
                }
                finally
                {
                    var di = new DirectoryInfo(folder);

                    var files = di.GetFiles($"{manifest.bundleGuid}*");
                    Array.ForEach(files, f => f.Delete());

                    files = di.GetFiles($"AssetBundles*");
                    Array.ForEach(files, f => f.Delete());

                }
            }
        }

        static long GetFileSize(string url)
        {
            var req = WebRequest.Create(url);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                if (long.TryParse(resp.Headers.Get("Content-Length"), out long result))
                {
                    return result;
                }
            }

            return -1;
        }


        static void SaveBundleLinks(string filename)
        {
            var gitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT");
            var gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
            var gitTag = Environment.GetEnvironmentVariable("GIT_TAG");
            var downloadHost = Environment.GetEnvironmentVariable("S3_DOWNLOAD_HOST");

            if (string.IsNullOrEmpty(gitCommit))
            {
                Debug.LogError("Cannot save bundle links - GIT_COMMIT is not set");
                return;
            }

            if (string.IsNullOrEmpty(downloadHost))
            {
                Debug.LogError("Cannot save bundle links - S3_DOWNLOAD_HOST is not set");
                return;
            }

            using (var f = File.CreateText(filename))
            {
                f.WriteLine("<html><body>");

                var dt = DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

                f.WriteLine("<h1>Info</h1>");
                f.WriteLine("<ul>");
                f.WriteLine($"<li>Build Date: {dt}</li>");
                if (!string.IsNullOrEmpty(gitCommit))
                {
                    f.WriteLine($"<li>Git Commit: {gitCommit}</li>");
                }
                if (!string.IsNullOrEmpty(gitBranch))
                {
                    f.WriteLine($"<li>Git Branch: {gitBranch}</li>");
                }
                if (!string.IsNullOrEmpty(gitTag))
                {
                    f.WriteLine($"<li>Git Tag: {gitTag}</li>");
                }
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Environments</h1>");
                f.WriteLine("<ul>");

                var simEnvironments = Environment.GetEnvironmentVariable("SIM_ENVIRONMENTS");
                if (!string.IsNullOrEmpty(simEnvironments))
                {
                    foreach (var line in simEnvironments.Split('\n'))
                    {
                        var items = line.Split(new[] { ' ' }, 2);
                        var id = items[0];
                        var name = items[1];

                        var url = $"https://{downloadHost}/{id}/environment_{name}";
                        var size = EditorUtility.FormatBytes(GetFileSize(url));
                        f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                    }
                }
                f.WriteLine("</ul>");

                f.WriteLine("<h1>Vehicles</h1>");
                f.WriteLine("<ul>");

                var simVehicles = Environment.GetEnvironmentVariable("SIM_VEHICLES");
                if (!string.IsNullOrEmpty(simVehicles))
                {
                    foreach (var line in simVehicles.Split('\n'))
                    {
                        var items = line.Split(new[] { ' ' }, 2);
                        var id = items[0];
                        var name = items[1];

                        var url = $"https://{downloadHost}/{id}/vehicle_{name}";
                        var size = EditorUtility.FormatBytes(GetFileSize(url));
                        f.WriteLine($"<li><a href='{url}'>{name}</a> ({size})</li>");
                    }
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

            var oldGraphicsJobSetting = PlayerSettings.graphicsJobs;
            try
            {
                UnityEditor.BuildTarget buildTarget;
                if (target == BuildTarget.Windows)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneWindows64;
                    PlayerSettings.graphicsJobs = true;
                }
                else if (target == BuildTarget.Linux)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneLinux64;
                    PlayerSettings.graphicsJobs = false;
                }
                else if (target == BuildTarget.MacOS)
                {
                    buildTarget = UnityEditor.BuildTarget.StandaloneOSX;
                    PlayerSettings.graphicsJobs = false;
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

                    var files = new[] { "index.html", "main.css", "main.js", "favicon.png" };
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
            finally
            {
                PlayerSettings.graphicsJobs = oldGraphicsJobSetting;
            }
        }

        static void Run()
        {
            List<string> environments = new List<string>();
            List<string> vehicles = new List<string>();
            BuildTarget? buildTarget = null;

            string buildPlayer = null;
            bool buildBundles = false;
            string saveBundleLinks = null;

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
                        environments.AddRange(args[i].Split(','));
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
                        vehicles.AddRange(args[i].Split(','));
                    }
                    else
                    {
                        throw new Exception("-buildVehicles expects comma seperated vehicle names!");
                    }
                }
                else if (args[i] == "-buildPlayer")
                {
                    if (i < args.Length - 1)
                    {
                        i++;
                        buildPlayer = args[i];
                    }
                    else
                    {
                        throw new Exception("-buildPlayer expects output folder!");
                    }
                }
                else if (args[i] == "-buildBundles")
                {
                    buildBundles = true;
                }
                else if (args[i] == "-developmentBuild")
                {
                    developmentBuild = true;
                }
            }

            if (!buildTarget.HasValue && !string.IsNullOrEmpty(buildPlayer))
            {
                throw new Exception("-buildTarget not specified!");
            }

            if (buildBundles)
            {
                var availEnvironments = new Dictionary<string, bool?>();
                var availVehicles = new Dictionary<string, bool?>();

                var external = Path.Combine(Application.dataPath, "External");
                Refresh(availEnvironments, Path.Combine(external, "Environments"), SceneExtension);
                Refresh(availVehicles, Path.Combine(external, "Vehicles"), PrefabExtension);

                if (environments.Count == 1 && environments[0] == "all")
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

                if (vehicles.Count == 1 && vehicles[0] == "all")
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

                if (environments.Count == 0 && vehicles.Count == 0)
                {
                    throw new Exception($"No environments or vehicles to build");
                }
            }

            Running = true;
            try
            {
                if (!string.IsNullOrEmpty(buildPlayer))
                {
                    RunPlayerBuild(buildTarget.Value, buildPlayer, developmentBuild);
                }

                if (buildBundles)
                {
                    var assetBundlesLocation = Path.Combine(Application.dataPath, "..", "AssetBundles");
                    RunAssetBundleBuild(assetBundlesLocation, environments, vehicles);
                }

                if (saveBundleLinks != null)
                {
                    SaveBundleLinks(saveBundleLinks);
                }
            }
            finally
            {
                Running = false;
            }
        }

        [MenuItem("Simulator/Build Sensors")]
        static void BuildSensors()
        {
            var outputFolder = Path.Combine(Application.dataPath, "..", "AssetBundles", "Sensors");
            Directory.CreateDirectory(outputFolder);

            var externalSensors = Path.Combine(Application.dataPath, "External", "Sensors");
            var directories = new DirectoryInfo(externalSensors).GetDirectories();

            var sensorsAsmDefPath = Path.Combine(externalSensors, "Sensors.asmdef");
            var sensorsAsmDef = JsonUtility.FromJson<AsmdefBody>(File.ReadAllText(sensorsAsmDefPath));

            foreach (var directoryInfo in directories)
            {
                string bundleGuid = Guid.NewGuid().ToString();
                string filename = directoryInfo.Name;

                try
                {
                    var prefab = Path.Combine("Assets", "External", "Sensors", filename, $"{filename}.prefab");
                    if (!File.Exists(Path.Combine(Application.dataPath, "..", prefab)))
                    {
                        Debug.LogError($"Building of {filename} failed: {prefab} not found");
                        break;
                    }

                    AsmdefBody asmdefContents = new AsmdefBody();
                    asmdefContents.name = filename;
                    asmdefContents.references = sensorsAsmDef.references;
                    File.WriteAllText(Path.Combine(externalSensors, filename, $"{filename}.asmdef"), JsonUtility.ToJson(asmdefContents));

                    Manifest manifest = new Manifest
                    {
                        assetName = filename,
                        bundleGuid = bundleGuid,
                        bundleFormat = BundleConfig.BundleFormatVersion,
                        description = "",
                        licenseName = "",
                        authorName = "",
                        authorUrl = "",
                    };

                    var windowsBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{bundleGuid}_sensor_main_windows",
                        assetNames = new[] { prefab },
                    };

                    BuildPipeline.BuildAssetBundles(
                            outputFolder,
                            new[] { windowsBuild },
                            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                            UnityEditor.BuildTarget.StandaloneWindows64);

                    if (!File.Exists(Path.Combine(outputFolder, windowsBuild.assetBundleName)))
                    {
                        Debug.LogError($"Failed to find Windows asset bundle of {filename}. Please correct other errors and try again.");
                        return;
                    }

                    var linuxBuild = new AssetBundleBuild()
                    {
                        assetBundleName = $"{bundleGuid}_sensor_main_linux",
                        assetNames = new[] { prefab },
                    };

                    BuildPipeline.BuildAssetBundles(
                            outputFolder,
                            new[] { linuxBuild },
                            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                            UnityEditor.BuildTarget.StandaloneLinux64);

                    if (!File.Exists(Path.Combine(outputFolder, linuxBuild.assetBundleName)))
                    {
                        Debug.LogError($"Failed to find Linux asset bundle of {filename}. Please correct other errors and try again.");
                        return;
                    }

                    DirectoryInfo prefabDir = new DirectoryInfo(Path.Combine(externalSensors, filename));
                    var scripts = prefabDir.GetFiles("*.cs", SearchOption.AllDirectories).Select(script => script.FullName).ToArray();

                    var outputAssembly = Path.Combine(outputFolder, $"{filename}.dll");
                    var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var modules = assemblies.Where(asm =>
                                                    asm.GetName().Name == "UnityEngine" ||
                                                    asm.GetName().Name == "UnityEngine.JSONSerializeModule" ||
                                                    asm.GetName().Name == "UnityEngine.CoreModule" ||
                                                    asm.GetName().Name == "UnityEngine.PhysicsModule").ToArray();

                    assemblyBuilder.additionalReferences = modules.Select(a => a.Location).ToArray();

                    assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
                    {
                        var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                        var warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                        try
                        {
                            Debug.Log($"Assembly build finished for {assemblyPath}");
                            if (errorCount != 0)
                            {
                                Debug.Log($"Found {errorCount} errors");

                                foreach (CompilerMessage message in compilerMessages)
                                {
                                    if (message.type == CompilerMessageType.Error)
                                    {
                                        Debug.LogError(message.message);
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                var manifestOutput = Path.Combine(outputFolder, "manifest");
                                File.WriteAllText(manifestOutput, new Serializer().Serialize(manifest));

                                using (ZipFile archive = ZipFile.Create(Path.Combine(outputFolder, $"sensor_{filename}")))
                                {
                                    archive.BeginUpdate();
                                    archive.Add(new StaticDiskDataSource(Path.Combine(outputFolder, linuxBuild.assetBundleName)), linuxBuild.assetBundleName, CompressionMethod.Stored, true);
                                    archive.Add(new StaticDiskDataSource(Path.Combine(outputFolder, windowsBuild.assetBundleName)), windowsBuild.assetBundleName, CompressionMethod.Stored, true);
                                    archive.Add(new StaticDiskDataSource(outputAssembly), $"{filename}.dll", CompressionMethod.Stored, true);
                                    archive.Add(new StaticDiskDataSource(manifestOutput), "manifest", CompressionMethod.Stored, true);
                                    archive.CommitUpdate();
                                    archive.Close();
                                }

                            }
                        }
                        finally
                        {
                            var di = new DirectoryInfo(outputFolder);
                            SilentDelete(Path.Combine(outputFolder, $"{filename}.dll"));
                            SilentDelete(Path.Combine(outputFolder, $"{filename}.pdb"));
                            SilentDelete(Path.Combine(outputFolder, "manifest"));
                        }
                    };

                    // Start build of assembly
                    if (!assemblyBuilder.Build())
                    {
                        Debug.LogErrorFormat("Failed to start build of assembly {0}!", assemblyBuilder.assemblyPath);
                        return;
                    }

                    while (assemblyBuilder.status != AssemblyBuilderStatus.Finished) { }
                }
                finally
                {
                    var di = new DirectoryInfo(outputFolder);

                    var files = di.GetFiles($"{bundleGuid}*");
                    Array.ForEach(files, f => SilentDelete(f.FullName));

                    SilentDelete(Path.Combine(outputFolder, "Sensors"));
                    SilentDelete(Path.Combine(outputFolder, "Sensors.manifest"));

                    SilentDelete(Path.Combine(externalSensors, filename, $"{filename}.asmdef"));
                    SilentDelete(Path.Combine(externalSensors, filename, $"{filename}.asmdef.meta"));
                }
            }
        }

        static void SilentDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
