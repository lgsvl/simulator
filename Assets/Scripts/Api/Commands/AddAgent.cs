/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Text;
using System.Linq;
using System;
using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;
using ICSharpCode.SharpZipLib.Zip;
using Simulator.Database;
using UnityEngine.SceneManagement;
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

        // We have to lock api.ActionsSemaphore before the first continuation (await)
        // to make sure API calls are executed one after the other
        public async void Execute(JSONNode args)
        {
            var sim = SimulatorManager.Instance;
            var api = ApiManager.Instance;
            // instead of relying on ApiMAnager's exception handling,
            // we wrap the whole method since we are async
            try
            {
                api.ActionsSemaphore.Lock();

                if (sim == null)
                {
                    throw new Exception("SimulatorManager not found! Is scene loaded?");
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

                    VehicleDetailData vehicleData = await ConnectionManager.API.GetByIdOrName<VehicleDetailData>(name);

                    var assetModel = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, vehicleData.AssetGuid, vehicleData.Name);

                    var prefab = AquirePrefab(vehicleData, assetModel);
                    if (prefab == null)
                    {
                        throw new Exception($"failed to acquire ego prefab");
                    }

                    var config = new AgentConfig()
                    {
                        AssetGuid = vehicleData.AssetGuid,
                        Name = vehicleData.Name,
                        Prefab = prefab,
                        Sensors = Newtonsoft.Json.JsonConvert.SerializeObject(vehicleData.Sensors),
                    };

                    if (vehicleData.bridge != null)
                    {
                        config.Bridge = Web.Config.Bridges.Find(bridge => bridge.Name == vehicleData.bridge.type);
                        if (config.Bridge == null)
                        {
                            throw new Exception($"Bridge '{vehicleData.bridge.type}' not available");
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

                    Debug.Assert(agentGO != null);
                    api.Agents.Add(uid, agentGO);
                    api.AgentUID.Add(agentGO, uid);

                    var sensors = agentGO.GetComponentsInChildren<SensorBase>(true);

                    foreach (var sensor in sensors)
                    {
                        var sensorUid = System.Guid.NewGuid().ToString();
                        if (SimulatorManager.InstanceAvailable)
                            SimulatorManager.Instance.Sensors.AppendUid(sensor, sensorUid);
                    }

                    api.SendResult(this, new JSONString(uid));
                    SIM.LogAPI(SIM.API.AddAgentEgo, name);
                }
                else if (type == (int)AgentType.Npc)
                {
                    var colorData = args["color"].ReadVector3();
                    var template = sim.NPCManager.NPCVehicles.Find(obj => obj.Prefab.name == name); // TODO need to search all available npcs including npc bundles
                    if (template.Prefab == null)
                    {
                        throw new Exception($"Unknown '{name}' NPC name");
                    }

                    var spawnData = new NPCManager.NPCSpawnData
                    {
                        Active = true,
                        GenId = uid,
                        Template = template,
                        Position = position,
                        Rotation = Quaternion.Euler(rotation),
                        Color = colorData == new Vector3(-1, -1, -1) ? sim.NPCManager.GetWeightedRandomColor(template.NPCType) : new Color(colorData.x, colorData.y, colorData.z),
                        Seed = sim.NPCManager.NPCSeedGenerator.Next(),
                    };

                    var npcController = SimulatorManager.Instance.NPCManager.SpawnNPC(spawnData);
                    npcController.SetBehaviour<NPCManualBehaviour>();

                    var body = npcController.GetComponent<Rigidbody>();
                    body.velocity = velocity;
                    body.angularVelocity = angular_velocity;

                    uid = npcController.name;
                    api.Agents.Add(uid, npcController.gameObject);
                    api.AgentUID.Add(npcController.gameObject, uid);
                    api.SendResult(this, new JSONString(uid));
                    SIM.LogAPI(SIM.API.AddAgentNPC, name);
                    // Override the color argument as NPCController may change the NPC color
                    if (Loader.Instance.Network.IsMaster)
                    {
                        var colorVector = new Vector3(npcController.NPCColor.r, npcController.NPCColor.g, npcController.NPCColor.b);
                        args["color"].WriteVector3(colorVector);
                    }
                }
                else if (type == (int)AgentType.Pedestrian)
                {
                    var pedManager = SimulatorManager.Instance.PedestrianManager;
                    if (!pedManager.gameObject.activeSelf)
                    {
                        var sceneName = SceneManager.GetActiveScene().name;
                        throw new Exception($"{sceneName} is missing Pedestrian NavMesh");
                    }

                    var model = sim.PedestrianManager.pedModels.Find(obj => obj.name == name);
                    if (model == null)
                    {
                        throw new Exception($"Unknown '{name}' pedestrian name");
                    }

                    var spawnData = new PedestrianManager.PedSpawnData
                    {
                        Active = true,
                        API = true,
                        GenId = uid,
                        Model = model,
                        Position = position,
                        Rotation = Quaternion.Euler(rotation),
                        Seed = sim.PedestrianManager.PEDSeedGenerator.Next(),
                    };

                    var pedController = pedManager.SpawnPedestrian(spawnData);
                    if (pedController == null)
                    {
                        throw new Exception($"Pedestrian controller error for '{name}'");
                    }

                    api.Agents.Add(uid, pedController.gameObject);
                    api.AgentUID.Add(pedController.gameObject, uid);
                    api.SendResult(this, new JSONString(uid));
                    SIM.LogAPI(SIM.API.AddAgentPedestrian, name);
                }
                else
                {
                    throw new Exception($"Unsupported '{args["type"]}' type");
                }
            }
            catch (Exception e)
            {
                api.SendError(this, e.Message);
            }
            finally
            {
                api.ActionsSemaphore.Unlock();
            }
        }

        public GameObject AquirePrefab(VehicleDetailData vehicle, AssetModel asset)
        {
            if (ApiManager.Instance.CachedVehicles.ContainsKey(vehicle.Name))
            {
                return ApiManager.Instance.CachedVehicles[vehicle.Name];
            }
            else
            {
                var bundlePath = asset.LocalPath;
                using (ZipFile zip = new ZipFile(bundlePath))
                {
                    Manifest manifest;
                    ZipEntry entry = zip.GetEntry("manifest.json");
                    using (var ms = zip.GetInputStream(entry))
                    {
                        int streamSize = (int)entry.Size;
                        byte[] buffer = new byte[streamSize];
                        streamSize = ms.Read(buffer, 0, streamSize);
                        manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(Encoding.UTF8.GetString(buffer));
                    }

                    if (manifest.assetFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Vehicle])
                    {
                        throw new Exception("Out of date Vehicle AssetBundle. Please check content website for updated bundle or rebuild the bundle.");
                    }

                    AssetBundle textureBundle = null;

                    if (zip.FindEntry($"{manifest.assetGuid}_vehicle_textures", true) != -1)
                    {
                        var texStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_textures"));
                        textureBundle = AssetBundle.LoadFromStream(texStream, 0, 1 << 20);
                    }

                    string platform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "windows" : "linux";
                    var mapStream = zip.GetInputStream(zip.GetEntry($"{manifest.assetGuid}_vehicle_main_{platform}"));
                    var vehicleBundle = AssetBundle.LoadFromStream(mapStream, 0, 1 << 20);

                    if (vehicleBundle == null)
                    {
                        throw new Exception($"Failed to load vehicle {vehicle.Name} from '{bundlePath}' asset bundle");
                    }

                    try
                    {
                        var vehicleAssets = vehicleBundle.GetAllAssetNames();
                        if (vehicleAssets.Length != 1)
                        {
                            throw new Exception($"Unsupported '{bundlePath}' vehicle asset bundle, only 1 asset expected");
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
