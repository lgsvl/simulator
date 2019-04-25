using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Simulator.Editor
{
    public class Build : EditorWindow
    {
        enum BuildTarget
        {
            Windows,
            Linux,
        }

        Vector2 EnvironmentScroll;
        Vector2 VehicleScroll;

        Dictionary<string, bool?> Environments = new Dictionary<string, bool?>();
        Dictionary<string, bool?> Vehicles = new Dictionary<string, bool?>();

        [SerializeField] BuildTarget Target;
        [SerializeField] bool BuildPlayer = true;
        [SerializeField] string PlayerFolder = string.Empty;
        [SerializeField] bool DevelopmentPlayer = false;

        [MenuItem("Simulator/Build...")]
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

            var data = EditorPrefs.GetString("Build", JsonUtility.ToJson(window, false));
            JsonUtility.FromJsonOverwrite(data, window);

            window.Show();
        }

        void OnDisable()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("Build", data);
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
                    GUILayout.Toggle(false, $"{name} (missing scene)");
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
                    GUILayout.Toggle(false, $"{name} (missing prefab)");
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Label("Options", EditorStyles.boldLabel);

            Target = (BuildTarget)EditorGUILayout.EnumPopup("Target OS:", Target);

            var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            BuildPlayer = GUILayout.Toggle(BuildPlayer, "Build Player:", GUILayout.ExpandWidth(false));

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
                    RunPlayerBuild(Target, PlayerFolder, DevelopmentPlayer);

                    assetBundlesLocation = Path.Combine(PlayerFolder, "AssetBundles");
                }

                var environments = Environments.Where(kv => kv.Value.HasValue && kv.Value.Value).Select(kv => kv.Key);
                var vehicles = Vehicles.Where(kv => kv.Value.HasValue && kv.Value.Value).Select(kv => kv.Key);

                RunAssetBundleBuild(Target, assetBundlesLocation, environments, vehicles);
            }
        }

        void OnFocus()
        {
            var external = Path.Combine(Application.dataPath, "External");

            Refresh(Environments, Path.Combine(external, "Environments"), "unity");
            Refresh(Vehicles, Path.Combine(external, "Vehicles"), "prefab");
        }

        void Refresh(Dictionary<string, bool?> items, string folder, string suffix)
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

        static void RunAssetBundleBuild(BuildTarget target, string folder, IEnumerable<string> environments, IEnumerable<string> vehicles)
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
                var web = Path.Combine(folder, "Web");
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
    }
}
