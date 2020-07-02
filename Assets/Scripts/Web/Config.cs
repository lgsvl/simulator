/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
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
using UnityEngine;
using VirtualFileSystem;
using YamlDotNet.Serialization;
using Simulator.Database;
using Simulator.Database.Services;

namespace Simulator.Web
{
    public static class Config
    {
        public static int sessionTimeout = 60*60*24*365;

        public static string ApiHost = "localhost";
        public static int ApiPort = 8181;

        public static bool RunAsMaster = true;
        
        public static string CloudUrl = "https://wise.lgsvlsimulator.com";
        public static string Username;
        public static string Password;
        public static string SessionGUID;
        public static string SimID;

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

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Initialize()
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

            LoadBuiltinAssets();
            LoadExternalAssets();
            Sensors = SensorTypes.ListSensorFields(SensorPrefabs);
            BridgePlugins.Load();

            ParseConfigFile();

            if (!Application.isEditor)
            {
                ParseCommandLine();
            }

            DatabaseManager.Init();

            ClientSettingsService csservice = new ClientSettingsService();
            if (string.IsNullOrEmpty(SimID))
            {
                SimID = csservice.GetOrMake().simid;
            }

            csservice.SetSimID(SimID);
        }

        public delegate void AssetLoadFunc(Manifest manifest, VfsEntry dir);

        private static void CheckDir(VfsEntry dir, AssetLoadFunc loadFunc)
        {
            if (dir == null)
            {
                return;
            }

            var manifestFile = dir.Find("manifest.json");
            if (manifestFile != null && manifestFile.IsFile)
            {
                Debug.Log($"found manifest at {manifestFile.Path}");
                using (var reader = new StreamReader(manifestFile.SeekableStream()))
                {
                    try
                    {
                        if (reader == null)
                        {
                            Debug.Log("no reader cannot open stream");
                        }
                        Manifest manifest;
                        try
                        {
                            var buffer = reader.ReadToEnd();
                            manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(buffer);
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
                    CheckDir(entry, loadFunc);
                }
            }
        }

        private static void LoadBuiltinAssets()
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

            var behaviours = new []
            {
                typeof(NPCLaneFollowBehaviour),
                typeof(NPCWaypointBehaviour),
                typeof(NPCManualBehaviour),
            };
            foreach (var b in behaviours)
            {
                NPCBehaviours.Add(b.ToString(), b);
            }
        }

        private static void SaveConfigFile()
        {
            try
            {
                File.WriteAllText(Path.Combine(Root, "config.yml"), new Serializer().Serialize(ConvertConfigFile()));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private static void LoadExternalAssets()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var dir = Path.Combine(Application.dataPath, "..", "AssetBundles");
            var vfs = VfsEntry.makeRoot(dir);

            // descend into each known dir looking for only specific asset types. todo: add asset type to manifest?
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Controllable)), LoadControllablePlugin);
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Bridge)), LoadBridgePlugin); // NOTE: bridges must be loaded before sensor plugins
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.Sensor)), LoadSensorPlugin);
            CheckDir(vfs.GetChild(BundleConfig.pluralOf(BundleConfig.BundleTypes.NPC)), LoadNPCAsset);

            Debug.Log($"Loaded {NPCBehaviours.Count} NPCs behaviours and {NPCVehicles.Count} NPC models in {sw.Elapsed}");
        }

        private static Assembly LoadAssembly(VfsEntry dir, string name)
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
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Bridge])
            {
                throw new Exception($"Manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Bridge]}, got {manifest.assetFormat}");
            }

            var pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");
            foreach (Type ty in pluginSource.GetTypes())
            {
                if (typeof(ISensorBridgePlugin).IsAssignableFrom(ty))
                {
                    var bridgeFactory = Activator.CreateInstance(ty) as IBridgeFactory;
                    BridgePlugins.Add(bridgeFactory);
                }
            }
        }

        public static void LoadSensorPlugin(Manifest manifest, VfsEntry dir) 
        {
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Sensor])
            {
                throw new Exception($"Manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Sensor]}, got {manifest.assetFormat}");
            }

            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");

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
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Controllable])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.Controllable]}, got {manifest.assetFormat}");
            }
            var texStream = dir.Find($"{manifest.assetGuid}_controllable_textures").SeekableStream();
            var textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);

            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");

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

        private static void LoadNPCAsset(Manifest manifest, VfsEntry dir)
        {
            if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.NPC])
            {
                throw new Exception($"manifest version mismatch, expected {BundleConfig.Versions[BundleConfig.BundleTypes.NPC]}, got {manifest.assetFormat}");
            }
            Assembly pluginSource = LoadAssembly(dir, $"{manifest.assetName}.dll");
            if (pluginSource != null)
            {
                foreach (Type ty in pluginSource.GetTypes())
                {
                    if (ty.IsAbstract)
                    {
                        continue;
                    }
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

            if (pluginEntry == null && pluginSource == null)
            {
                Debug.LogError("Neither assembly nor prefab found in "+manifest.assetName);
            }

            if (textureBundle && !AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
            {
                textureBundle.LoadAllAssets();
            }
        }

        private class YamlConfig
        {
            public bool headless { get; set; } = Config.Headless;
            public bool client { get; set; } = !Config.RunAsMaster;
            public bool read_only { get; set; } = false;
            public string api_hostname { get; set; } = Config.ApiHost;
            public int api_port { get; set; } = Config.ApiPort;
            public string cloud_url { get; set; } = Config.CloudUrl;
            public string data_path { get; set; } = Config.PersistentDataPath;
        }

        private static YamlConfig LoadConfigFile(string file)
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

        private static YamlConfig ConvertConfigFile()
        {
            return new YamlConfig()
            {
                api_hostname = ApiHost,
                api_port = ApiPort,
                data_path = PersistentDataPath,
                cloud_url = CloudUrl,
                client = !RunAsMaster,
                headless = Headless
            };
        }

        public static void ParseConfigFile()
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

            ApiHost = config.api_hostname ?? "localhost";
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

        private static void ParseCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--simid":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for sim id provided!");
                            Application.Quit(1);
                        }
                        SimID = args[++i];
                        break;
                    case "--cloudurl":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for cloud url provided!");
                            Application.Quit(1);
                        }
                        CloudUrl = args[++i];
                        break;
                    case "--apihost":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for api hostname provided!");
                            Application.Quit(1);
                        }
                        ApiHost = args[++i];
                        break;
                    case "--apiport":
                        if (i == args.Length - 1)
                        {
                            Debug.LogError("No value for api port provided!");
                            Application.Quit(1);
                        }

                        if (!int.TryParse(args[++i], out ApiPort))
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
