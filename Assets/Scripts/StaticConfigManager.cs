/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using YamlDotNet.Serialization;
using YamlDotNet.RepresentationModel;

//public class StaticConfig
//{
//    public bool initialized = false;
//    public InitialConfiguration initial_configuration { get; set; }
//    public List<VehicleConfig> vehicles { get; set; }
//};

//public class InitialConfiguration
//{
//    public string map { get; set; }
//    public float time_of_day { get; set; }
//    public bool freeze_time_of_day { get; set; }
//    public float fog_intensity { get; set; }
//    public float rain_intensity { get; set; }
//    public float road_wetness { get; set; }
//    public bool enable_traffic { get; set; }
//    public bool enable_pedestrian { get; set; }
//    public int traffic_density { get; set; }
//}

//public class VehicleConfig
//{
//    public string type { get; set; } //: XE_Rigged-autoware
//    public string address { get; set; }
//    public int port { get; set; }
//    public string command_type { get; set; } //: twist
//    public bool enable_lidar { get; set; }
//    public bool enable_gps { get; set; }
//    public bool enable_imu { get; set; }
//    public bool enable_main_camera { get; set; }
//    public bool enable_telephoto_camera { get; set; }
//    public bool enable_sensor_effects { get; set; }
//    public bool enable_high_quality_rendering { get; set; }
//    public PositionVector position { get; set; }
//    public OrientationVector orientation { get; set; }
//}

//public class PositionVector
//{
//    public float n { get; set; }
//    public float e { get; set; }
//    public float h { get; set; }
//}

//public class OrientationVector
//{
//    public float r { get; set; }
//    public float p { get; set; }
//    public float y { get; set; }
//}

public class StaticConfigManager : MonoBehaviour
{
    #region Singleton
    private static StaticConfigManager _instance = null;
    public static StaticConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<StaticConfigManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>StaticConfigManager" + " Not Found!</color>");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
            DestroyImmediate(gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }
    #endregion
    
    public StaticConfig staticConfig = new StaticConfig();
    public bool isFirstStart = true;

    public GameObject rosAgent;
    
    private void Start()
    {
        if (FindObjectOfType<ROSAgentManager>() == null)
        {
            Instantiate(rosAgent);
        }
        if (isFirstStart) ReadStaticConfigFile();

        if (staticConfig.initialized && isFirstStart)
        {
            //ShowFreeRoaming();
            //OnRunClick();
            //isFirstStart = false; // UserInterfaceSetup.cs sets this
        }
    }

    public void LoadStaticConfigScene()
    {

    }

    void ReadStaticConfigFile()
    {
        var configFile = "";
        if (!Application.isEditor)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" && args.Length > i + 1)
                {
                    configFile = args[i + 1];
                }
            }
        }
        else
        {
            // uncomment to test static config in Editor
            //configFile = "static_config_sample.yaml";
        }

        if (!String.IsNullOrEmpty(configFile))
        {
            StreamReader reader = new StreamReader(configFile);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            staticConfig = deserializer.Deserialize<StaticConfig>(reader);

            // need map and at least one vehicle specified in the static config
            if (!String.IsNullOrEmpty(staticConfig.initial_configuration.map) && staticConfig.vehicles.Count > 0)
            {
                Debug.Log("Static config map: " + staticConfig.initial_configuration.map + " vehicle: " + staticConfig.vehicles[0].type);
                staticConfig.initialized = true;

                //Robots.Robots.Clear();
                //var candidate = Robots.robotCandidates[0];

                //foreach (var staticVehicle in staticConfig.vehicles)
                //{
                //    foreach (var rob in Robots.robotCandidates)
                //    {
                //        if (rob.name == staticVehicle.type)
                //        {
                //            candidate = rob;
                //            break;
                //        }
                //    }

                //    Robots.Robots.Add(new RosBridgeConnector(staticVehicle.address, staticVehicle.port, candidate));
                //}
            }

        }
        UserInterfaceSetup.staticConfig = staticConfig;
    }
}
