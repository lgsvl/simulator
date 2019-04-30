using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Simulator.Editor
{
    public class Check : EditorWindow
    {
        static readonly Dictionary<string, string[]> UnityFolders = new Dictionary<string, string[]>()
        {
            // folder => allowed extensions
            { "Animations", new [] { ".controller", ".anim", ".playable" } },
            { "Audio", new [] { ".wav", ".mp3" } },
            { "Effects", new [] { ".png" } },
            { "GlobalSettings", new [] { ".asset" } },
            { "Materials", new [] { ".mat", ".png", ".jpg" } },
            { "Models", new [] { ".fbx" } },
            { "Physics", new [] { ".physicMaterial" } },
            { "Pefabs", new [] { ".prefab" } },
            { "Resources", new [] { ".txt", ".prefab" } }, // TODO: does this need prefab?
            { "ScriptableObjects", new [] { ".asset" } },
            { "Scenes", new [] { ".unity" } },
            { "Shaders", new [] { ".shader" } },
            { "Textures", new [] { ".png", ".exr", ".jpg" } },
        };

        public enum Category
        {
            Info,
            Warning,
            Error,
        };

        GUIStyle Style;
        Vector2 ScrollPosition;
        string Output;

        [MenuItem("Simulator/Check...")]
        static void ShowWindow()
        {
            var window = GetWindow<Check>();
            window.Style = new GUIStyle(EditorStyles.textField);
            window.Style.richText = true;
            window.Show();
        }

        void OnEnable()
        {
            RunCheck();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Check", GUILayout.ExpandWidth(false)))
            {
                RunCheck();
            }

            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            GUILayout.TextArea(Output, Style, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        void RunCheck()
        {
            Output = string.Empty;
            RunCheck((category, message) =>
            {
                if (category == Category.Error)
                {
                    Output += $"<color=red><b>ERROR:</b></color> {message}\n";
                }
                else if (category == Category.Warning)
                {
                    Output += $"<color=yellow><b>WARNING:</b></color> {message}\n";
                }
                else
                {
                    Output += $"{message}\n";
                }
            });
        }

        static void RunCheck(Action<Category, string> log)
        {
            log(Category.Info, "Checking...");

            var rootFolders = new[]
            {
                // allowed generated folders
                "AssetBundles",
                "Library",
                "obj",
                "Temp",
                "Web",
            };

            var rootFoldersReq = new[]
            {
                // required folders
                "Assets",
                "Packages",
                "ProjectSettings",
                "WebUI",
            };

            var rootPath = Path.Combine(Application.dataPath, "..");

            CheckFolders(log, "/", rootPath, rootFolders, rootFoldersReq, false);
            CheckExtensions(log, "/ProjectSettings", Path.Combine(rootPath, "ProjectSettings"), new[] { ".asset", ".txt" });

            CheckAssets(log, Path.Combine(rootPath, "Assets"));

            CheckMainDependencies(log, "Assets/Scenes/LoaderScene.unity");

            log(Category.Info, "Done!");
        }

        static void CheckAssets(Action<Category, string> log, string assetsFolder)
        {
            var assetFolders = new[]
            {
                // allowed folders
                "Animations",
                "Audio",
                "Effects",
                "GlobalSettings",
                "Materials",
                "Meshes",
                "Physics",
                "Plugins",
                "Resources",
                "ScriptableObjects",
                "Scenes",
                "Settings",
                "Shaders",
                "Textures",
            };

            var assetFoldersReq = new[]
            {
                // required folders
                "External",
                "Scripts",
            };

            var assetFiles = new[]
            {
                // allowed files
                "csc.rsp",
            };

            CheckFolders(log, "/Assets", assetsFolder, assetFolders, assetFoldersReq, true);
            CheckFiles(log, "/Assets", assetsFolder, assetFiles, Array.Empty<string>(), true);

            CheckScripts(log, "/Assets/Scripts", Path.Combine(assetsFolder, "Scripts"));
            CheckPlugins(log, "/Assets/Plugins", Path.Combine(assetsFolder, "Plugins"));

            var environments = Path.Combine(assetsFolder, "External", "Environments");
            if (Directory.Exists(environments))
            {
                foreach (var environment in Directory.EnumerateDirectories(environments))
                {
                    CheckEnvironment(log, environment);
                }
            }

            var vehicles = Path.Combine(assetsFolder, "External", "Vehicles");
            if (Directory.Exists(vehicles))
            {
                foreach (var vehicle in Directory.EnumerateDirectories(vehicles))
                {
                    CheckVehicle(log, vehicle);
                }
            }

            CheckMetaFiles(log, "/Assets", assetsFolder);
        }

        static void CheckEnvironment(Action<Category, string> log, string environment)
        {
            var name = Path.GetFileName(environment);
            var folderName = $"/Assets/External/Environments/{name}";
            var scene = Path.Combine(environment, $"{name}.unity");

            if (File.Exists(scene))
            {
                CheckExternalDependencies(log, folderName, $"{folderName}/{name}.unity");
            }
            else
            {
                log(Category.Error, $"Environment scene '{folderName}/{name}.unity' does not exist");
            }

            var folders = new[]
            {
                "Animations",
                "Materials",
                "Media",
                "Models",
                "Prefabs",
                "Scenes",
                "Shaders",
                "Textures",
            };

            CheckFolders(log, folderName, environment, folders, Array.Empty<string>(), true);

            var models = Path.Combine(environment, "Models");
            if (Directory.Exists(models))
            {
                foreach (var model in Directory.EnumerateDirectories(models))
                {
                    var modelName = Path.GetFileName(model);
                    var modelFolder = $"{folderName}/Models/{modelName}";

                    CheckExtensions(log, modelFolder, model, new[] { ".fbx" });
                    CheckFolders(log, modelFolder, model, new[] { "Materials" }, Array.Empty<string>(), true);
                }
            }
        }

        static void CheckVehicle(Action<Category, string> log, string vehicle)
        {
            var name = Path.GetFileName(vehicle);
            var folderName = $"/Assets/External/Vehicles/{name}";
            var prefab = Path.Combine(vehicle, $"{name}.prefab");

            if (File.Exists(prefab))
            {
                CheckExternalDependencies(log, folderName, $"{folderName}/{name}.prefab");
            }
            else
            {
                log(Category.Error, $"Vehicle prefab '{folderName}/{name}.prefab' does not exist");
            }

            var folders = new[]
            {
                "Animations",
                "Media",
                "Models",
                "Prefabs",
                "Scenes",
                "Shaders",
            };

            CheckFolders(log, folderName, vehicle, folders, Array.Empty<string>(), true);

            var models = Path.Combine(vehicle, "Models");
            if (Directory.Exists(models))
            {
                foreach (var model in Directory.EnumerateDirectories(models))
                {
                    var modelName = Path.GetFileName(model);
                    var modelFolder = $"{folderName}/Models/{modelName}";

                    CheckExtensions(log, modelFolder, model, new[] { ".fbx" });
                    CheckFolders(log, modelFolder, model, new[] { "Materials" }, Array.Empty<string>(), true);
                }
            }
        }

        static void CheckMetaFiles(Action<Category, string> log, string folderName, string folder)
        {
            var metas = new HashSet<string>();

            // each folder should have meta file
            bool empty = true;
            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                empty = false;
                var name = Path.GetFileName(f);
                if (name.StartsWith("."))
                {
                    continue;
                }

                var meta = Path.Combine(folder, name) + ".meta";
                var targetFolder = $"{folderName}/{name}";
                if (!File.Exists(meta))
                {
                    log(Category.Error, $"Meta file '{name}.meta' does not exist for '{targetFolder}' folder");
                }
                CheckMetaFiles(log, targetFolder, Path.Combine(folder, name));

                metas.Add(name);
            }

            // each file should have meta file
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                empty = false;
                var name = Path.GetFileName(f);
                if (name.StartsWith("."))
                {
                    continue;
                }
                if (Path.GetExtension(name) == ".meta")
                {
                    continue;
                }

                var meta = Path.Combine(folder, name) + ".meta";
                if (!File.Exists(meta))
                {
                    log(Category.Error, $"Meta file '{name}.meta' does not exist for '{folderName}/{name}' file");
                }
                metas.Add(name);
            }

            if (empty)
            {
                log(Category.Warning, $"Folder '{folderName}' is empty");
            }

            // there should be no other meta files left
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (Path.GetExtension(name) != ".meta")
                {
                    continue;
                }
                var metaName = Path.GetFileNameWithoutExtension(name);
                if (!metas.Contains(metaName))
                {
                    log(Category.Error, $"Meta file '{folderName}/{name}' should be deleted");
                }
            }
        }

        static void CheckScripts(Action<Category, string> log, string folderName, string folder)
        {
            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith("."))
                {
                    CheckScripts(log, $"{folderName}/{name}", Path.Combine(folder, name));
                }
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                var extension = Path.GetExtension(f);
                if (name.StartsWith("."))
                {
                    continue;
                }
                if (extension == ".meta")
                {
                    continue;
                }
                if (folderName == "/Assets/Scripts" && name == "csc.rsp")
                {
                    continue;
                }

                if (extension != ".cs")
                {
                    log(Category.Error, $"File '{name}' does not have '.cs' extension inside '{folderName}' folder");
                }
            }
        }

        static void CheckPlugins(Action<Category, string> log, string folderName, string folder)
        {
            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta")
                {
                    log(Category.Error, $"File '{name}' is not allowed inside '{folderName}'");
                }
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                var files = Directory.EnumerateFiles(f);

                bool hasLicense = files.Count(fname => fname.ToLowerInvariant().Contains("license")) != 0;
                if (!hasLicense)
                {
                    log(Category.Warning, $"Plugin '{folderName}/{name}' does not have license file");
                }

                // TODO: extra checks for native plugins?
            }
        }

        static void CheckUnityFolders(Action<Category, string> log, string folderName, string folder)
        {
            var name = Path.GetFileName(folder);
            string[] extensions;
            if (UnityFolders.TryGetValue(name, out extensions))
            {
                CheckExtensions(log, folderName, folder, extensions);
            }
        }

        static void CheckFolders(Action<Category, string> log, string folderName, string folder, string[] allowedFolders, string[] requiredFolders, bool error)
        {
            var found = new HashSet<string>();

            if (!Directory.Exists(folder))
            {
                log(Category.Error, $"Folder '{folderName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith("."))
                {
                    if (allowedFolders.Contains(name) || requiredFolders.Contains(name))
                    {
                        CheckUnityFolders(log, $"{folderName}/{name}", Path.Combine(folder, name));
                    }
                    else
                    {
                        log(error ? Category.Error : Category.Warning, $"Folder '{name}' should not be inside of '{folderName}'");
                    }
                    found.Add(name);
                }
            }

            foreach (var f in requiredFolders)
            {
                if (!found.Contains(f))
                {
                    log(Category.Error, $"Folder '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        static void CheckFiles(Action<Category, string> log, string folderName, string folder, string[] allowedFiles, string[] requiredFiles, bool error)
        {
            var found = new HashSet<string>();

            if (!Directory.Exists(folder))
            {
                log(Category.Error, $"Folder '{folderName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateFiles(folder))
            {
                var name = Path.GetFileName(f);
                if (!name.StartsWith(".") && Path.GetExtension(f) != ".meta")
                {
                    if (!allowedFiles.Contains(name) && !requiredFiles.Contains(name))
                    {
                        log(error ? Category.Error : Category.Warning, $"File '{name}' should not be inside '{folderName}'");
                    }
                    found.Add(name);
                }
            }

            foreach (var f in requiredFiles)
            {
                if (!found.Contains(f))
                {
                    log(Category.Error, $"File '{f}' does not exist inside '{folderName}'");
                }
            }
        }

        static void CheckExtensions(Action<Category, string> log, string rootName, string root, string[] allowed)
        {
            if (!Directory.Exists(root))
            {
                log(Category.Error, $"Folder '{rootName}' does not exist");
                return;
            }

            foreach (var f in Directory.EnumerateFiles(root))
            {
                var name = Path.GetFileName(f);
                var ext = Path.GetExtension(f);
                if (!name.StartsWith(".") && ext != ".meta")
                {
                    if (!allowed.Contains(ext))
                    {
                        log(Category.Error, $"File '{name}' with '{ext}' extension is not allowed inside '{rootName}'");
                    }
                }
            }
        }

        public static void CheckMainDependencies(Action<Category, string> log, string scene)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scene.Substring(1), true))
            {
                if (dep.StartsWith("Packages/"))
                {
                    continue;
                }
                if (dep.StartsWith("Assets/") && !dep.StartsWith("Assets/External/"))
                {
                    continue;
                }
                log(Category.Error, $"Main scene depends on '/{dep}'");
            }
        }

        public static void CheckExternalDependencies(Action<Category, string> log, string externalFolder, string externalAsset)
        {
            externalFolder = externalFolder.Substring(1);
            externalAsset = externalAsset.Substring(1);

            var dependencies = new List<string>();
            foreach (var dep in AssetDatabase.GetDependencies(externalAsset, true))
            {
                if (dep.StartsWith("Packages/") || dep.StartsWith("Assets/GlobalSettings"))
                {
                    continue;
                }
                if (dep.StartsWith("Assets/Scripts/"))
                {
                    continue;
                }
                if (dep.StartsWith($"{externalFolder}/"))
                {
                    continue;
                }
                dependencies.Add(dep);
            }

            dependencies.Sort();
            foreach (var dep in dependencies)
            {
                log(Category.Error, $"Asset '/{externalAsset}' depends on '/{dep}'");
            }
        }
    }
}
