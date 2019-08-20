/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using Simulator.Bridge;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        public static bool AgreeToLicense = false;

        public static bool Headless = false;

        public static string Root;
        public static string PersistentDataPath;

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
            Sensors = SensorTypes.ListSensorFields(RuntimeSettings.Instance?.SensorPrefabs);
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
                if (args[i] == "--hostname" || args[i] == "-h")
                {
                    if (i == args.Length - 1)
                    {
                        Debug.LogError("No value for hostname provided!");
                        Application.Quit(1);
                    }
                    WebHost = args[++i];
                }
                else if (args[i] == "--port" || args[i] == "-p")
                {
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
                }
                else if (args[i] == "--slave" || args[i] == "-s")
                {
                    RunAsMaster = false;
                }
                else if (args[i] == "--master" || args[i] == "-m")
                {
                    RunAsMaster = true;
                }
                else if (args[i] == "--username" || args[i] == "-u")
                {
                    if (i == args.Length - 1)
                    {
                        Debug.LogError("No value for username provided!");
                        Application.Quit(1);
                    }
                    Username = args[++i];
                }
                else if (args[i] == "--password" || args[i] == "-w")
                {
                    if (i == args.Length - 1)
                    {
                        Debug.LogError("No value for password provided!");
                        Application.Quit(1);
                    }
                    Password = args[++i];
                }
                else if (args[i] == "--agree")
                {
                    AgreeToLicense = true;
                }
                else
                {
                    Debug.LogError($"Unknown argument {args[i]}");
                    Application.Quit(1);
                }
            }
        }
    }
}
