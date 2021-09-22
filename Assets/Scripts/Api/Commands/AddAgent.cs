/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Simulator.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace Simulator.Api.Commands
{
    enum AgentType
    {
        Unknown = 0,
        Ego = 1,
        Npc = 2,
        Pedestrian = 3,
    };

    class AddAgent : IDistributedCommand, ILockingCommand
    {
        public string Name => "simulator/add_agent";
        public event Action<ILockingCommand> Executed;

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
                    {
                        args.Add("uid", uid);
                    }
                }
                else
                {
                    uid = argsUid.Value;
                }

                if (type == (int)AgentType.Ego)
                {
                    var agents = SimulatorManager.Instance.AgentManager;
                    GameObject agentGO = null;

                    VehicleDetailData vehicleData = await ConnectionManager.API.GetByIdOrName<VehicleDetailData>(name);
                    var config = new AgentConfig(vehicleData.ToVehicleData());
                    config.Position = position;
                    config.Rotation = Quaternion.Euler(rotation);

                    if (ApiManager.Instance.CachedVehicles.ContainsKey(vehicleData.Name))
                    {
                        config.Prefab = ApiManager.Instance.CachedVehicles[vehicleData.Name];
                    }
                    else
                    {
                        var assetModel = await DownloadManager.GetAsset(BundleConfig.BundleTypes.Vehicle, vehicleData.AssetGuid, vehicleData.Name);
                        config.Prefab = Loader.LoadVehicleBundle(assetModel.LocalPath);
                    }

                    if (config.Prefab == null)
                    {
                        throw new Exception($"failed to acquire ego prefab");
                    }

                    var downloads = new List<Task>();
                    List<SensorData> sensorsToDownload = new List<SensorData>();
                    ConcurrentDictionary<Task, string> assetDownloads = new ConcurrentDictionary<Task, string>();

                    if (config.Sensors != null)
                    {
                        foreach (var plugin in config.Sensors)
                        {
                            if (plugin.Plugin.AssetGuid != null && sensorsToDownload.FirstOrDefault(s => s.Plugin.AssetGuid == plugin.Plugin.AssetGuid) == null)
                            {
                                sensorsToDownload.Add(plugin);
                            }
                        }
                    }

                    if (config.BridgeData != null)
                    {
                        var pluginTask = DownloadManager.GetAsset(BundleConfig.BundleTypes.Bridge, config.BridgeData.AssetGuid, config.BridgeData.Name);
                        downloads.Add(pluginTask);
                        assetDownloads.TryAdd(pluginTask, config.BridgeData.Type);
                    }

                    foreach (var sensor in sensorsToDownload)
                    {
                        var pluginTask = DownloadManager.GetAsset(BundleConfig.BundleTypes.Sensor, sensor.Plugin.AssetGuid,
                            sensor.Name);
                        downloads.Add(pluginTask);
                        assetDownloads.TryAdd(pluginTask, sensor.Type);
                    }

                    await Task.WhenAll(downloads);
                    foreach (var download in downloads)
                    {
                        assetDownloads.TryRemove(download, out _);
                    }

                    agentGO = agents.SpawnAgent(config);

                    if (agents.ActiveAgents.Count == 1)
                    {
                        agents.SetCurrentActiveAgent(agentGO);
                        var hdCamera = HDCamera.GetOrCreate(SimulatorManager.Instance.CameraManager.SimulatorCamera);
                        hdCamera.RequestExposureAdaptationLock();
                    }

                    var rb = agentGO.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = velocity;
                        rb.angularVelocity = angular_velocity;
                    }

                    Debug.Assert(agentGO != null);
                    api.Agents.Add(uid, agentGO);
                    api.AgentUID.Add(agentGO, uid);

                    var sensors = agentGO.GetComponentsInChildren<SensorBase>(true);

                    foreach (var sensor in sensors)
                    {
                        var sensorUid = System.Guid.NewGuid().ToString();
                        if (SimulatorManager.InstanceAvailable)
                        {
                            SimulatorManager.Instance.Sensors.AppendUid(sensor, sensorUid);
                        }
                    }

                    api.SendResult(this, new JSONString(uid));
                }
                else if (type == (int)AgentType.Npc)
                {
                    var colorData = args["color"].ReadVector3();
                    var template = sim.NPCManager.NPCVehicles.Find(obj => obj.Prefab.name == name);
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
                    npcController.IsUserSpecified = true;
                    npcController.SetBehaviour<NPCManualBehaviour>();

                    var body = npcController.GetComponent<Rigidbody>();
                    body.velocity = velocity;
                    body.angularVelocity = angular_velocity;

                    uid = npcController.name;
                    api.Agents.Add(uid, npcController.gameObject);
                    api.AgentUID.Add(npcController.gameObject, uid);
                    api.SendResult(this, new JSONString(uid));

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

                    var model = sim.PedestrianManager.PedestrianData.Find(obj => obj.Name == name).Prefab;
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
                }
                else
                {
                    throw new Exception($"Unsupported '{args["type"]}' type");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                api.SendError(this, e.Message);
            }
            finally
            {
                Executed?.Invoke(this);
            }
        }
    }
}
