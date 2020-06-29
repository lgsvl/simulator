/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Bridge;
using Simulator.Controllable;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VirtualFileSystem;
using YamlDotNet.Serialization;

namespace Simulator.Web
{
    public static class Config
    {
        public static string WebHost = "localhost";
        public static int WebPort = 8080;

        public static int sessionTimeout = 60*60*24*365;

        public static string ApiHost = WebHost;
        public static int ApiPort = 8181;

        public static bool RunAsMaster = true;

        public static string CloudUrl = "https://account.lgsvlsimulator.com";
        public static string Username;
        public static string Password;
        public static string SessionGUID;
        public static bool AgreeToLicense = false;

        public static bool Headless = false;

        public static string Root;
        public static string PersistentDataPath;

        public static List<SensorBase> SensorPrefabs;
        public static List<SensorConfig> Sensors;

        public static Dictionary<string, IControllable> Controllables = new Dictionary<string, IControllable>();
        public static Dictionary<string, Type> NPCBehaviours = new Dictionary<string, Type>();

        public struct NPCAssetData
        {
            public GameObject prefab;
            public Map.NPCSizeType NPCType;
            public string Name;
            public string AssetGuid;
        }
        public static Dictionary<string, NPCAssetData> NPCVehicles = new Dictionary<string, NPCAssetData>();

        public static Dictionary<string, IntPtr> FMUs = new Dictionary<string, IntPtr>(); // managed by FMU.cs

        public static int DefaultPageSize = 100;

        public static byte[] salt { get; set; }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static void Initialize()
        {
            Root = Path.Combine(Application.dataPath, "..");
            PersistentDataPath = Application.persistentDataPath;
            AssetBundle.UnloadAllAssetBundles(false);
            SensorPrefabs = RuntimeSettings.Instance.SensorPrefabs.ToList();

            if (SensorPrefabs.Any(s=> s == null))
            {
                Debug.LogError("Null Sensor Prefab Detected - Check RuntimeSettings SensorPrefabs List for missing Sensor Prefab");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                // return non-zero exit code
                Application.Quit(1);
#endif
                return;
            }

            BridgePlugins.Load();
            LoadBuiltinAssets();
            LoadExternalAssets();
            Sensors = SensorTypes.ListSensorFields(SensorPrefabs);

            ParseConfigFile();
            if (!Application.isEditor)
            {
                ParseCommandLine();
            }
        }

        public delegate void AssetLoadFunc(Manifest manifest, VfsEntry dir);

        static void checkDir(VfsEntry dir, AssetLoadFunc loadFunc)
        {
            if(dir == null) return;
            var manifestFile = dir.Find("manifest");
            if (manifestFile != null && manifestFile.IsFile)
            {
                Debug.Log($"found manifest at {manifestFile.Path}");
                using(var reader = new StreamReader(manifestFile.SeekableStream())) {
                    try
                    {
                        if(reader == null) Debug.Log("no reader?");
                        Manifest manifest;
                        try
                        {
                            manifest = new Deserializer().Deserialize<Manifest>(reader);
                        }
                        catch
                        {
                            throw new Exception("Out of date AssetBundle, rebuild or download latest AssetBundle.");
                        }
                        loadFunc(manifest, dir);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"failed to load asset from {manifestFile.Path}: {ex.Message} STACK: {ex.StackTrace}");
                    }
                }
            }
            else
            {
                foreach (var entry in dir)
                {
                    checkDir(entry, loadFunc);
                }
            }

        }

        static void LoadBuiltinAssets()
        {
            var npcSettings = NPCSettings.Load();
            var prefabs = new[]
            {
                "Hatchback",
                "Sedan",
                "Jeep",
                "SUV",
                "BoxTruck",
                "SchoolBus",
            };
            foreach (var entry in prefabs)
            {
                var go = npcSettings.NPCPrefabs.Find(x => x.name == entry) as GameObject;
                if (go == null)
                {
                    // I was seeing this in editor a few times, where it was not able to find the builtin assets
                    Debug.LogError($"Failed to load builtin {entry} "+(go==null?"null":go.ToString()));
                    continue;
                }
                Map.NPCSizeType size = Map.NPCSizeType.MidSize;
                var meta = go.GetComponent<NPCMetaData>();

                if (meta != null)
                {
                    size = meta.SizeType;
                }
                else
                {
                    Debug.LogWarning($"NPC {entry} missing meta info, setting default size");
                }

                NPCVehicles.Add(entry, new NPCAssetData
                {
                    prefab = go,
                    NPCType = size,
                    Name = entry,
                    AssetGuid = $"builtin-{entry}",
                });
            }

            var behaviours = new[]{
                typeof(NPCLaneFollowBehaviour),
                typeof(NPCWaypointBehaviour),
                typeof(NPCManualBehaviour),
            };
            foreach (var b in behaviours)
            {
                NPCBehaviours.Add(b.ToString(), b);
            }
        }

        static void LoadExternalAssets()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var dir = Path.Combine(Application.dataPath, "..", "AssetBundles");
            var vfs = VfsEntry.makeRoot(dir);
            // descend into each known dir looking for only specific asset types. todo: add asset type to manifest?
            checkDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Controllable)), LoadControllablePlugin);
            checkDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Bridge)), LoadBridgePlugin); // NOTE: bridges must be loaded before sensor plugins
            checkDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Sensor)), LoadSensorPlugin);
            checkDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC)), loadNPCAsset);

            Debug.Log($"Loaded {NPCBehaviours.Count} NPCs behaviours and {NPCVehicles.Count} NPC models in {sw.Elapsed}");
        }

        private static Assembly loadAssembly(VfsEntry dir, string name)
        {
            var dll = dir.Find(name);
            if (dll == null)
            {
                return null;
            }
            byte[] buffer = new byte[dll.Size];
            dll.GetStream().Read(buffer, 0, (int)dll.Size);
            return Assembly.Load(buffer);
        }

        public static void LoadBridgePlugin(Manifest manifest, VfsEntry dir)
        {
            if (manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Bridge])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Sensor]}, got {manifest.bundleFormat}");
            }

            var pluginSource = loadAssembly(dir, $"{manifest.assetName}.dll");
            foreach (Type ty in pluginSource.GetTypes())
            {
                if (typeof(IBridgeFactory).IsAssignableFrom(ty))
                {
                    var bridgeFactory = Activator.CreateInstance(ty) as IBridgeFactory;
                    BridgePlugins.Add(bridgeFactory);
                }
            }
        }

        public static void LoadSensorPlugin(Manifest manifest, VfsEntry dir) 
        {
            if (manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Sensor])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Sensor]}, got {manifest.bundleFormat}");
            }

            Assembly pluginSource = loadAssembly(dir, $"{manifest.assetName}.dll");

            foreach (Type ty in pluginSource.GetTypes())
            {
                if (typeof(ISensorBridgePlugin).IsAssignableFrom(ty))
                {
                    var sensorBridgePlugin = Activator.CreateInstance(ty) as ISensorBridgePlugin;
                    foreach (var kv in BridgePlugins.All)
                    {
                        sensorBridgePlugin.Register(kv.Value);
                    }
                }
            }

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginStream = dir.Find($"{manifest.assetGuid}_sensor_main_{platform}").SeekableStream();
            AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginStream);
            var pluginAssets = pluginBundle.GetAllAssetNames();
            SensorPrefabs.Add(pluginBundle.LoadAsset<GameObject>(pluginAssets[0]).GetComponent<SensorBase>());
        }

        public static void LoadControllablePlugin(Manifest manifest, VfsEntry dir) 
        {
            if(manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Controllable]) {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Controllable]}, got {manifest.bundleFormat}");
            }
            var texStream = dir.Find($"{manifest.assetGuid}_controllable_textures").SeekableStream();
            var textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);

            Assembly pluginSource = loadAssembly(dir, $"{manifest.assetName}.dll");

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginStream = dir.Find($"{manifest.assetGuid}_controllable_main_{platform}").SeekableStream();
            AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginStream);
            var pluginAssets = pluginBundle.GetAllAssetNames();
            if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
            {
                textureBundle.LoadAllAssets();
            }
            Controllables.Add(manifest.assetName, pluginBundle.LoadAsset<GameObject>(pluginAssets[0]).GetComponent<IControllable>());
        }

        private static void loadNPCAsset(Manifest manifest, VfsEntry dir)
        {
            if (manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.NPC])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.NPC]}, got {manifest.bundleFormat}");
            }
            Assembly pluginSource = loadAssembly(dir, $"{manifest.assetName}.dll");
            if (pluginSource != null)
            {
                foreach (Type ty in pluginSource.GetTypes())
                {
                    if(ty.IsAbstract) continue;
                    if (typeof(NPCBehaviourBase).IsAssignableFrom(ty))
                    {
                        NPCBehaviours.Add(ty.ToString(), ty);
                    }
                    else if (typeof(ICommand).IsAssignableFrom(ty))
                    {
                        var cmd = Activator.CreateInstance(ty) as ICommand;
                        ApiManager.Commands.Add(cmd.Name, cmd);
                    }
                }
            }
            var texEntry = dir.Find($"{manifest.assetGuid}_npc_textures");
            AssetBundle textureBundle = null;
            if (texEntry != null)
            {
                var texStream = VirtualFileSystem.VirtualFileSystem.EnsureSeekable(texEntry.SeekableStream(), (int)texEntry.Size);
                textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
            }

            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
            var pluginEntry = dir.Find($"{manifest.assetGuid}_npc_main_{platform}");
            if (pluginEntry != null)
            {

                AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginEntry.SeekableStream());
                var pluginAssets = pluginBundle.GetAllAssetNames();
                GameObject prefab = pluginBundle.LoadAsset<GameObject>(pluginAssets[0]);

                Map.NPCSizeType size = Map.NPCSizeType.MidSize;
                var meta = prefab.GetComponent<NPCMetaData>();

                if (meta != null)
                {
                    size = meta.SizeType;
                }
                else
                {
                    Debug.LogWarning($"NPC {manifest.assetName} missing meta info, setting default type");
                }

                NPCVehicles.Add(manifest.assetName, new NPCAssetData()
                {
                    prefab = prefab,
                    Name = manifest.assetName,
                    AssetGuid = manifest.assetGuid,
                    NPCType = size,
                });
            }

            if(pluginEntry == null && pluginSource == null)
            {
                Debug.LogError("Neither assembly nor prefab found in "+manifest.assetName);
            }

            if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
            {
                textureBundle.LoadAllAssets();
            }
        }

        class YamlConfig
        {
            public string hostname { get; set; } = Config.WebHost;
            public int port { get; set; } = Config.WebPort;
            public bool headless { get; set; } = Config.Headless;
            public bool client { get; set; } = !Config.RunAsMaster;
            public bool read_only { get; set; } = false;
            public string api_hostname { get; set; } = Config.ApiHost;
            public int api_port { get; set; } = Config.ApiPort;
            public string cloud_url { get; set; } = Config.CloudUrl;
            public string data_path { get; set; } = Config.PersistentDataPath;
        }

        static YamlConfig LoadConfigFile(string file)
        {
            using (var fs = File.OpenText(file))
            {
                try
                {
                    return new Deserializer().Deserialize<YamlConfig>(fs);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            return null;
        }


        static void ParseConfigFile()
        {
            var configFile = Path.Combine(Root, "config.yml");
            if (!File.Exists(configFile))
            {
                return;
            }

            var config = LoadConfigFile(configFile);
            if (config == null)
            {
                return;
            }

            WebHost = config.hostname;
            WebPort = config.port;

            ApiHost = config.api_hostname ?? WebHost;
            ApiPort = config.api_port;

            PersistentDataPath = config.data_path;

            CloudUrl = config.cloud_url;
            string cloudUrl = Environment.GetEnvironmentVariable("SIMULATOR_CLOUDURL");
            if (!string.IsNullOrEmpty(cloudUrl))
            {
                CloudUrl = cloudUrl;
            }

            RunAsMaster = !config.client;
            Headless = config.headless;
        }

        static void ParseCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--hostname":
                    case "-h":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for hostname provided!");
                            Application.Quit(1);
                        }
                        WebHost = args[++i];
                        break;
                    case "--port":
                    case "-p":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for port provided!");
                            Application.Quit(1);
                        }
                        if (!int.TryParse(args[++i], out WebPort))
                        {
                            Debug.LogError("Port must be an integer!");
                            Application.Quit(1);
                        }

                        break;
                    case "--client":
                    case "-c":
                        RunAsMaster = false;
                        break;
                    case "--master":
                    case "-m":
                        RunAsMaster = true;
                        break;
                    case "--username":
                    case "-u":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for username provided!");
                            Application.Quit(1);
                        }

                        Username = args[++i];
                        break;
                    case "--password":
                    case "-w":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for password provided!");
                            Application.Quit(1);
                        }

                        Password = args[++i];
                        break;
                    case "--data":
                    case "-d":
                        if(i == args.Length - 1)
                        {
                            Debug.LogError("No value for data path provided!");
                            Application.Quit(1);
                        }

                        PersistentDataPath = args[++i];
                        break;
                    case "--agree":
                        AgreeToLicense = true;
                        break;
                    default:
                        // skip unknown arguments to allow to pass default Unity Player args
                        Debug.LogError($"Unknown argument {args[i]}, skipping it");
                        break;
                }
            }
        }
    }
}
