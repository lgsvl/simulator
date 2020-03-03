/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Text;
using System.Linq;
using PetaPoco;
using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;
using YamlDotNet.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Database;
using UnityEngine.SceneManagement;
using Simulator.Network.Core.Identification;
using Simulator.Web;

namespace Simulator.Api.Commands
{
    enum AgentType
    {
        Unknown = 0,
        Ego = 1,
        Npc = 2,
        Pedestrian = 3,
    };

    class AddAgent : IDistributedCommand
    {
        public string Name => "simulator/add_agent";

        public void Execute(JSONNode args)
        {
            var sim = Object.FindObjectOfType<SimulatorManager>();
            var api = ApiManager.Instance;

            if (sim == null)
            {
                api.SendError("SimulatorManager not found! Is scene loaded?");
                return;
            }

            var name = args["name"].Value;
            var type = args["type"].AsInt;
            var position = args["state"]["transform"]["position"].ReadVector3();
            var rotation = args["state"]["transform"]["rotation"].ReadVector3();
            var velocity = args["state"]["velocity"].ReadVector3();
            var angular_velocity = args["state"]["angular_velocity"].ReadVector3();

            string uid;
            var argsUid = args["uid"];
            if (argsUid == null)
            {
                uid = System.Guid.NewGuid().ToString();
                // Add uid key to arguments, as it will be distributed to the clients' simulations
                if (Loader.Instance.Network.IsMaster)
                    args.Add("uid", uid);
            }
            else
                uid = argsUid.Value;
            
            if (type == (int)AgentType.Ego)
            {
                var agents = SimulatorManager.Instance.AgentManager;
                GameObject agentGO = null;

                using (var db = DatabaseManager.Open())
                {
                    var sql = Sql.Builder.From("vehicles").Where("name = @0", name);
                    var vehicle = db.FirstOrDefault<VehicleModel>(sql);
                    if (vehicle == null)
                    {
                        var url = args["url"];
                        //Disable using url on master simulation
                        if (Loader.Instance.Network.IsMaster || string.IsNullOrEmpty(url))
                        {
                            api.SendError($"Vehicle '{name}' is not available");
                            return;
                        }
                        
                        DownloadVehicleFromUrl(args, name, url);
                        return;
                    }
                    else
                    {
                        var prefab = AquirePrefab(vehicle);
                        if (prefab == null)
                        {
                            return;
                        }

                        var config = new AgentConfig()
                        {
                            Name = vehicle.Name,
                            Prefab = prefab,
                            Sensors = vehicle.Sensors,
                        };

                        if (!string.IsNullOrEmpty(vehicle.BridgeType))
                        {
                            config.Bridge = Web.Config.Bridges.Find(bridge => bridge.Name == vehicle.BridgeType);
                            if (config.Bridge == null)
                            {
                                api.SendError($"Bridge '{vehicle.BridgeType}' not available");
                                return;
                            }
                        }

                        agentGO = agents.SpawnAgent(config);
                        agentGO.transform.position = position;
                        agentGO.transform.rotation = Quaternion.Euler(rotation);

                        if (agents.ActiveAgents.Count == 1)
                        {
                            agents.SetCurrentActiveAgent(agentGO);
                        }

                        var rb = agentGO.GetComponent<Rigidbody>();
                        rb.velocity = velocity;
                        rb.angularVelocity = angular_velocity;
                        // Add url key to arguments, as it will be distributed to the clients' simulations
                        if (Loader.Instance.Network.IsMaster)
                            args.Add("url", vehicle.Url);
                    }
                }

                Debug.Assert(agentGO != null);
                api.Agents.Add(uid, agentGO);
                api.AgentUID.Add(agentGO, uid);

                var sensors = agentGO.GetComponentsInChildren<SensorBase>();

                foreach (var sensor in sensors)
                {
                    var sensor_uid = System.Guid.NewGuid().ToString();
                    api.SensorUID.Add(sensor, sensor_uid);
                    api.Sensors.Add(sensor_uid, sensor);
                }

                api.SendResult(new JSONString(uid));
                SIM.LogAPI(SIM.API.AddAgentEgo, name);
            }
            else if (type == (int)AgentType.Npc)
            {
                var go = SimulatorManager.Instance.NPCManager.SpawnVehicle(name, uid, position, Quaternion.Euler(rotation));

                var npc = go.GetComponent<NPCController>();
                npc.Control = NPCController.ControlType.Manual;

                var body = go.GetComponent<Rigidbody>();
                body.velocity = velocity;
                body.angularVelocity = angular_velocity;

                uid = go.name;
                api.Agents.Add(uid, go);
                api.AgentUID.Add(go, uid);
                api.SendResult(new JSONString(go.name));
                SIM.LogAPI(SIM.API.AddAgentNPC, name);
            }
            else if (type == (int)AgentType.Pedestrian)
            {
                var pedManager = SimulatorManager.Instance.PedestrianManager;
                if (!pedManager.gameObject.activeSelf)
                {
                    var sceneName = SceneManager.GetActiveScene().name;
                    api.SendError($"{sceneName} is missing Pedestrian NavMesh");
                    return;
                }
                var ped = pedManager.SpawnPedestrianApi(name, uid, position, Quaternion.Euler(rotation));
                if (ped == null)
                {
                    api.SendError($"Unknown '{name}' pedestrian name");
                    return;
                }

                api.Agents.Add(uid, ped);
                api.AgentUID.Add(ped, uid);

                api.SendResult(new JSONString(uid));
                SIM.LogAPI(SIM.API.AddAgentPedestrian, name);
            }
            else
            {
                api.SendError($"Unsupported '{args["type"]}' type");
            }
        }

        private void DownloadVehicleFromUrl(JSONNode args, string name, string url)
        {
            //Remove url from args, so download won't be retried
            args.Remove("url");
            var localPath = WebUtilities.GenerateLocalPath("Vehicles");
            DownloadManager.AddDownloadToQueue(new System.Uri(url), localPath, null,
                (success, ex) =>
                {
                    if (success)
                    {
                        var vehicleModel = new VehicleModel()
                        {
                            Name = name,
                            Url = url,
                            BridgeType = args["bridge_type"],
                            LocalPath = localPath,
                            Sensors = null
                        };
                        using (var db = DatabaseManager.Open())
                        {
                            db.Insert(vehicleModel);
                        }
                        Execute(args);
                    }
                    else
                    {
                        Debug.LogError($"Vehicle '{name}' is not available. Error occured while downloading from url: {ex}.");
                        ApiManager.Instance.SendError($"Vehicle '{name}' is not available");
                    }
                });
        }

        public GameObject AquirePrefab(VehicleModel vehicle)
        {
            if (ApiManager.Instance.CachedVehicles.ContainsKey(vehicle.Name))
            {
                return ApiManager.Instance.CachedVehicles[vehicle.Name];
            }
            else
            {
                var bundlePath = vehicle.LocalPath;
                using (ZipFile zip = new ZipFile(bundlePath))
                {
                    Manifest manifest;
                    ZipEntry entry = zip.GetEntry("manifest");
                    using (var ms = zip.GetInputStream(entry))
                    {
                        int streamSize = (int)entry.Size;
                        byte[] buffer = new byte[streamSize];
                        streamSize = ms.Read(buffer, 0, streamSize);
                        manifest = new Deserializer().Deserialize<Manifest>(Encoding.UTF8.GetString(buffer, 0, streamSize));
                    }

                    if (manifest.bundleFormat != BundleConfig.VehicleBundleFormatVersion)
                    {
                        ApiManager.Instance.SendError("Out of date Vehicle AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                        return null;
                    }

                    AssetBundle textureBundle = null;

                    if (zip.FindEntry($"{manifest.bundleGuid}_vehicle_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.bundleGuid}_vehicle_main_{platform}"));
                    var vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (vehicleBundle == null)
                    {
                        ApiManager.Instance.SendError($"Failed to load vehicle from '{bundlePath}' asset bundle");
                        return null;
                    }

                    try
                    {
                        var vehicleAssets = vehicleBundle.GetAllAssetNames();
                        if (vehicleAssets.Length != 1)
                        {
                            ApiManager.Instance.SendError($"Unsupported '{bundlePath}' vehicle asset bundle, only 1 asset expected");
                            return null;
                        }

                        if (!AssetBundle.GetAllLoadedAssetBundles().Contains(textureBundle))
                        {
                            textureBundle?.LoadAllAssets();
                        }

                        var prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                        ApiManager.Instance.CachedVehicles.Add(vehicle.Name, prefab);
                        return prefab;
                    }
                    finally
                    {
                        textureBundle?.Unload(false);
                        vehicleBundle.Unload(false);
                    }
                }
            }
        }
    }
}
