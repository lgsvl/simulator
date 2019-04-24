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
        enum TargetOS
        {
            Windows,
            Linux,
        }

        class Item
        {
            public string Name;
            public string AssetPath;
            public bool Checked;
        }

        Dictionary<string, Item> Environments = new Dictionary<string, Item>();
        Dictionary<string, Item> Vehicles = new Dictionary<string, Item>();
        TargetOS Target;

        [MenuItem("Simulator/Build")]
        static void ShowWindow()
        {
            var window = GetWindow<Build>();
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                window.Target = TargetOS.Windows;
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                window.Target = TargetOS.Linux;
            }
            else

                window.Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
            {
                Refresh();
            }

            GUILayout.Label("Environments", EditorStyles.boldLabel);
            foreach (var path in Environments.Keys.OrderBy(path => Environments[path].Name))
            {
                var item = Environments[path];

                var name = item.Name;
                var check = item.Checked;
                if (item.AssetPath == null)
                {
                    name = $"{name} (missing scene)";
                    check = false;
                }

                EditorGUI.BeginDisabledGroup(item.AssetPath == null);
                item.Checked = GUILayout.Toggle(check, name);
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Label("Vehicles", EditorStyles.boldLabel);
            foreach (var path in Vehicles.Keys.OrderBy(path => Vehicles[path].Name))
            {
                var item = Vehicles[path];

                var name = item.Name;
                var check = item.Checked;
                if (item.AssetPath == null)
                {
                    name = $"{name} (missing prefab)";
                    check = false;
                }

                EditorGUI.BeginDisabledGroup(item.AssetPath == null);
                item.Checked = GUILayout.Toggle(check, name);
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Label("Options", EditorStyles.boldLabel);

            Target = (TargetOS)EditorGUILayout.EnumPopup("Target OS:", Target);

            if (GUILayout.Button("Build...", GUILayout.ExpandWidth(false)))
            {
                BuildTarget target;
                if (Target == TargetOS.Windows)
                {
                    target = BuildTarget.StandaloneWindows64;
                }
                else if (Target == TargetOS.Linux)
                {
                    target = BuildTarget.StandaloneLinux64;
                }
                else
                {
                    throw new Exception($"Unsupported Operating System ({Target})");
                }

                Run(target);
            }
        }

        void OnFocus()
        {
            Refresh();
        }

        void Refresh()
        {
            var external = Path.Combine(Application.dataPath, "External");

            Refresh(Environments, Path.Combine(external, "Environments"), "unity");
            Refresh(Vehicles, Path.Combine(external, "Vehicles"), "prefab");
        }

        void Refresh(Dictionary<string, Item> items, string folder, string suffix)
        {
            var updated = new HashSet<string>();

            foreach (var path in Directory.EnumerateDirectories(folder))
            {
                if (!items.ContainsKey(path))
                {
                    var name = Path.GetFileName(path);
                    var fullPath = Path.Combine(path, $"{name}.{suffix}");
                    var assetPath = RelativePath(Application.dataPath, fullPath);
                    items.Add(path, new Item()
                    {
                        Name = name,
                        AssetPath = File.Exists(fullPath) ? assetPath : null,
                        Checked = true,
                    });
                }

                updated.Add(path);
            }

            var removed = new List<string>();
            foreach (var kv in items)
            {
                if (!updated.Contains(kv.Key))
                {
                    removed.Add(kv.Key);
                }
            }

            removed.ForEach(remove => items.Remove(remove));
        }

        static string RelativePath(string root, string path)
        {
            var rootUri = new Uri(root, UriKind.Absolute);
            var pathUri = new Uri(path, UriKind.Absolute);

            return rootUri.MakeRelativeUri(pathUri).ToString();
        }

        void Run(BuildTarget target)
        {
            var builds = new List<AssetBundleBuild>();
            foreach (var kv in Environments)
            {
                var path = kv.Key;
                var environment = kv.Value;
                if (environment.Checked)
                {
                    builds.Add(new AssetBundleBuild()
                    {
                        assetBundleName = $"environment_{environment.Name}",
                        assetNames = new[] { environment.AssetPath },
                    });
                }
            }

            foreach (var kv in Vehicles)
            {
                var path = kv.Key;
                var vehicle = kv.Value;
                if (vehicle.Checked)
                {
                    builds.Add(new AssetBundleBuild()
                    {
                        assetBundleName = $"vehicle_{vehicle.Name}",
                        assetNames = new[] { vehicle.AssetPath },
                    });
                }
            }

            if (builds.Count == 0)
            {
                Debug.LogWarning("No items selected for build!");
                return;
            }

            var bundles = Path.Combine(Application.dataPath, "..", "AssetBundles");
            Directory.CreateDirectory(bundles);

            BuildPipeline.BuildAssetBundles(
                bundles,
                builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                target);
        }
    }
}
