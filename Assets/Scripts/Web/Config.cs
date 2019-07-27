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
        public static string WebBindHost = "localhost";
        public static int WebBindPort = 8080;

        public static int DefaultPageSize = 100;

        public static string ApiHost = WebBindHost;
        public static int ApiPort = 8181;

        public static bool RunAsMaster = true;

        public static string Username;
        public static string Password;

        public static bool Headless = false;
        public static bool AgreeToLicense = false;

        public static string Root;
        public static string PersistentDataPath;

        public static List<SensorConfig> Sensors;
        public static List<IBridgeFactory> Bridges;

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
            public string hostname { get; set; } = "localhost";
            public int port { get; set; } = 9090;
            public bool headless { get; set; } = false;
            public bool client { get; set; } = false;
            public bool read_only { get; set; } = false;
            public string api_hostname { get; set; }
            public int api_port { get; set; } = 8181;
            public string cloud_url { get; set; }
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

            WebBindHost = config.hostname;
            WebBindPort = config.port;

            ApiHost = config.api_hostname ?? WebBindHost;
            ApiPort = config.api_port;

            RunAsMaster = !config.client;
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
                    WebBindHost = args[++i];
                }
                else if (args[i] == "--port" || args[i] == "-p")
                {
                    if (i == args.Length - 1)
                    {
                        Debug.LogError("No value for port provided!");
                        Application.Quit(1);
                    }
                    if (!int.TryParse(args[++i], out WebBindPort))
                    {
                        Debug.LogError("Port must be an integer!");
                        Application.Quit(1);
                    }
                }
                else if (args[i] == "--client" || args[i] == "-c")
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
                else if (args[i] == "--password" || args[i] == "-u")
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
