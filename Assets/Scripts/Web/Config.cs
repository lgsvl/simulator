/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Bridge;
using Simulator.Sensors;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
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
        public static List<IBridgeFactory> Bridges;

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
            SensorPrefabs = LoadSensorPlugins();
            Sensors = SensorTypes.ListSensorFields(SensorPrefabs);
            Bridges = BridgeTypes.GetBridgeTypes();

            ParseConfigFile();
            if (!Application.isEditor)
            {
                ParseCommandLine();
            }
        }

        class YamlConfig
        {
            public string hostname { get; set; } = Config.WebHost;
            public int port { get; set; } = Config.WebPort;
            public bool headless { get; set; } = Config.Headless;
            public bool slave { get; set; } = !Config.RunAsMaster;
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

        public static List<SensorBase> LoadSensorPlugins()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            List<SensorBase> prefabs = RuntimeSettings.Instance.SensorPrefabs.ToList();
            if (prefabs.Any(s=> s == null))
            {
                Debug.LogError("Null Sensor Prefab Detected - Check RuntimeSettings SensorPrefabs List for missing Sensor Prefab");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                // return non-zero exit code
                Application.Quit(1);
#endif
                return null;
            }

            var sensorDirectory = Path.Combine(Application.dataPath, "..", "AssetBundles", "Sensors");
            if (Directory.Exists(sensorDirectory))
            {
                DirectoryInfo dir = new DirectoryInfo(sensorDirectory);
                foreach (FileInfo file in dir.GetFiles())
                {
                    try
                    {
                        var path = file.FullName;
                        using (ZipFile zip = new ZipFile(path))
                        {
                            string manfile = Encoding.UTF8.GetString(GetFile(zip, "manifest"));
                            Manifest manifest = new Deserializer().Deserialize<Manifest>(manfile);
                            Debug.Log($"Loading {manifest.assetName}");
                            if (manifest.bundleFormat != BundleConfig.SensorBundleFormatVersion)
                            {
                                throw new Exception("BundleFormat version mismatch");
                            }

                            System.Reflection.Assembly.Load(GetFile(zip, $"{manifest.assetName}.dll"));
                            string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                            var pluginStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_sensor_main_{platform}"));
                            AssetBundle pluginBundle = AssetBundle.LoadFromStream(pluginStream);
                            var pluginAssets = pluginBundle.GetAllAssetNames();
                            prefabs.Add(pluginBundle.LoadAsset<GameObject>(pluginAssets[0]).GetComponent<SensorBase>());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"Failed to load sensor plugin {file.FullName}, skipping it.");
                    }
                }
            }

            return prefabs;
        }

        static byte[] GetFile(ZipFile zip, string entryName)
        {
            var entry = zip.GetEntry(entryName);
            int streamSize = (int)entry.Size;
            byte[] buffer = new byte[streamSize];
            zip.GetInputStream(entry).Read(buffer, 0, streamSize);
            return buffer;
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

            RunAsMaster = !config.slave;
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
                    case "--slave":
                    case "-s":
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
