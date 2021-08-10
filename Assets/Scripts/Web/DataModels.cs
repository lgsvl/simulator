/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using static Simulator.Web.Config;

namespace Simulator.Web
{

    public static class JsonSettings
    {
        public static JsonSerializerSettings camelCase = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        public static JsonSerializerSettings camelCasePretty = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CloudData : Attribute
    {
        public string ApiPath { get; set; }
    }

    public class CloudIdData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CloudAssetDetails : CloudIdData
    {
        public string AssetGuid { get; set; }
        public bool IsShared { get; set; }
        public bool IsFavored { get; set; }
        public bool IsOwned { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Version { get; set; }
        public string LicenseName { get; set; }
        public string AuthorName { get; set; }
        public string AuthorUrl { get; set; }
        public string Copyright { get; set; }
        public string AccessType { get; set; } // "e.g. "public"
        public TagData[] Tags { get; set; }
        public UserData Owner { get; set; }
        public UserData[] FavoredBy { get; set; }
        public UserData[] SharedWith { get; set; }
    }

    public class MapData : CloudIdData
    {
        public string AssetGuid { get; set; }
    }

    [CloudData(ApiPath = "api/v1/maps")]
    public class MapDetailData : CloudAssetDetails
    {
        public string SupportedPlatforms { get; set; }
        public string Hdmaps { get; set; }
    }

    public class TagData
    {
        public string Name;
    }

    public class UserData
    {
        public string id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class VehicleData : CloudIdData
    {
        public string AssetGuid { get; set; }
        public SensorData[] Sensors { get; set; }
        public BridgeData Bridge { get; set; }
        public SensorsConfiguration[] SensorsConfigurations { get; set; }
    }

    public class SensorsConfiguration : CloudIdData
    {
        public bool IsOwned { get; set; }
        public string VehicleId { get; set; }
        public string BridgePluginId { get; set; }
        public BridgeData Bridge { get; set; }
        public SensorData[] Sensors { get; set; }
    }

    [CloudData(ApiPath = "api/v1/vehicles")]
    public class VehicleDetailData : CloudAssetDetails
    {
        public SensorData[] Sensors { get; set; }
        public SensorsConfiguration[] SensorsConfigurations { get; set; }
        public BridgeData Bridge { get; set; }
        public string Fmu { get; set; }
        public string BridgePluginId { get; set; }

        public VehicleData ToVehicleData()
        {
            return new VehicleData
            {
                Id = Id,
                Name = Name,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                OwnerId = OwnerId,
                AssetGuid = AssetGuid,
                Bridge = Bridge,
                Sensors = Sensors,
                SensorsConfigurations = SensorsConfigurations
            };
        }
    }

    [CloudData(ApiPath = "api/v1/plugins")]
    public class PluginDetailData : CloudAssetDetails
    {
        public string Category { get; set; } // e.g. "sensor"
        public string Type { get; set; } // e.g. "Comfort"
    }

    public class BridgeData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AssetGuid { get; set; }
        public string Type { get; set; }
        public string ConnectionString { get; set; }
    }

    public class ClusterData : CloudIdData
    {
        public InstanceData[] Instances { get; set; }
    }

    public class InstanceData
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public string SimId { get; set; }
        public string MacAddress { get; set; }
        public string Platform { get; set; } //e.g. "Unix 4.15.0.96"
        public string[] Ip { get; set; }
        public string Version { get; set; } // e.g. "2020.05",
        public bool IsMaster { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // e.g. "online"
    }

    public class SensorData
    {
        public string Name;
        public string Parent;
        public string Type;
        public TransformData Transform;
        public SensorPlugin Plugin;
        public Dictionary<string, object> @params;
    }

    public class SensorPlugin
    {
        public string Id;
        public string Type;
        public string AssetGuid;
    }

    public class TransformData
    {
        public float x;
        public float y;
        public float z;
        public float pitch;
        public float yaw;
        public float roll;
    }

    public class TemplateParameter
    {
        public String Alias { get; set; }
        public String ParameterName { get; set; }
        public String ParameterType { get; set; }
        public String VariableName { get; set; }
        public String VariableType { get; set; }

        [JsonProperty("value")]
        public JToken RawValue { get; set; }

        public T GetValue<T>()
        {
            try
            {
                if (RawValue is JValue)
                {
                    // Cast plain value
                    var value = RawValue as JValue;
                    return value.Value<T>();
                }
                else
                {
                    // Cast generic JToken to object
                    return RawValue.ToObject<T>();
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to cast {RawValue.GetType()} template parameter value '{RawValue}' to {typeof(T)}: {e}");
                throw;
            }
        }
    }

    public class TemplateData
    {
        public string Alias { get; set; }
        public TemplateParameter[] Parameters;
        public SimulationRunner Runner;
    }

    public class RunnerDockerAuth
    {
        public string Username;
        public string Password;
        public string PullToken;
    }

    public class RunnerDocker
    {
        public string Image;
        public RunnerDockerAuth Auth;
    }

    public class SimulationRunner
    {
        public RunnerDocker Docker;
    }


    [System.Serializable] // required for developerSettings
    public class SimulationData : CloudIdData
    {
        public string Version { get; set; }
        public bool ApiOnly { get; set; }
        public bool Interactive { get; set; }
        public bool Headless { get; set; }
        public DateTime TimeOfDay { get; set; }
        public float Rain { get; set; }
        public float Fog { get; set; }
        public float Wetness { get; set; }
        public float Cloudiness { get; set; }
        public float Damage { get; set; }
        public int? Seed { get; set; }
        public bool UseTraffic { get; set; }
        public bool UseBicyclists { get; set; }
        public bool UsePedestrians { get; set; }
        public MapData Map { get; set; }
        public VehicleData[] Vehicles { get; set; }
        public ClusterData Cluster { get; set; }
        public TemplateData Template { get; set; }
        public string TestReportId { get; set; }
        public NPCAssetData[] NPCs { get; set; }
        public PedAssetData[] Peds { get; set; }
    }


    public class LibraryList<DetailData>
    {
        public int Count { get; set; }
        public DetailData[] Rows { get; set; }
    }
}
