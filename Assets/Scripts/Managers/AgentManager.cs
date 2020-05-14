/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator;
using Simulator.Sensors;
using Simulator.Utilities;
using Simulator.Components;
using Simulator.Database;
using SimpleJSON;
using PetaPoco;
using YamlDotNet.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Simulator.Network.Core;
using Simulator.Network.Core.Components;
using Simulator.Network.Core.Messaging;
using Simulator.FMU;
using Simulator.Network.Shared;
using UnityEngine.Rendering.HighDefinition;

public class AgentManager : MonoBehaviour
{
    private MessagesManager networkMessagesManager;
    public string Key { get; } = "AgentManager";
    
    public GameObject CurrentActiveAgent { get; private set; } = null;
    public AgentController CurrentActiveAgentController { get; private set; } = null;
    public List<AgentConfig> ActiveAgents { get; private set; } = new List<AgentConfig>();

    public MessagesManager NetworkMessagesManager
    {
        get
        {
            if (networkMessagesManager == null) 
                networkMessagesManager = Loader.Instance.Network.MessagesManager;
            return networkMessagesManager;
        }
    }

    public event Action<GameObject> AgentChanged;

    public GameObject SpawnAgent(AgentConfig config)
    {
        var go = Instantiate(config.Prefab, transform);
        go.name = config.Name;
        var agentController = go.GetComponent<AgentController>();
        agentController.SensorsChanged += AgentControllerOnSensorsChanged;
        agentController.Config = config;
        agentController.Config.AgentGO = go;
        SIM.LogSimulation(SIM.Simulation.VehicleStart, config.Name);
        
        ActiveAgents.Add(agentController.Config);
        agentController.GTID = ++SimulatorManager.Instance.GTIDs;
        agentController.Config.GTID = agentController.GTID;

        BridgeClient bridgeClient = null;
        if (config.Bridge != null)
        {
            bridgeClient = go.AddComponent<BridgeClient>();
            bridgeClient.Init(config.Bridge);

            if (config.Connection != null)
            {
                var split = config.Connection.Split(':');
                if (split.Length != 2)
                {
                    throw new Exception("Incorrect bridge connection string, expected HOSTNAME:PORT");
                }
                bridgeClient.Connect(split[0], int.Parse(split[1]));
            }
        }
        SIM.LogSimulation(SIM.Simulation.BridgeTypeStart, config.Bridge != null ? config.Bridge.Name : "None");
        var sensorsController = go.AddComponent<SensorsController>();
        agentController.AgentSensorsController = sensorsController;
        sensorsController.SetupSensors(config.Sensors);

        //Add required components for distributing rigidbody from master to clients
        var network = Loader.Instance.Network;
        if (network.IsClusterSimulation)
        {
            HierarchyUtilities.ChangeToUniqueName(go);
            if (network.IsClient)
            {
                //Disable controller and dynamics on clients so it will not interfere mocked components
                agentController.enabled = false;
                var vehicleDynamics = agentController.GetComponent<IVehicleDynamics>() as MonoBehaviour;
                if (vehicleDynamics != null)
                    vehicleDynamics.enabled = false;
            }
            
            //Change the simulation type only if it's not set in the prefab
            var distributedRigidbody = go.GetComponent<DistributedRigidbody>();
            if (distributedRigidbody == null)
            {
                distributedRigidbody = go.AddComponent<DistributedRigidbody>();
                distributedRigidbody.SimulationType = DistributedRigidbody.MockingSimulationType.ExtrapolateVelocities;
            }

            //Add the rest required components for cluster simulation
            ClusterSimulationUtilities.AddDistributedComponents(go);
        }

        go.transform.position = config.Position;
        go.transform.rotation = config.Rotation;
        agentController.Init();

#if UNITY_EDITOR
        // TODO remove hack for editor opaque with alpha clipping 2019.3.3
        Array.ForEach(go.GetComponentsInChildren<Renderer>(), renderer =>
        {
            foreach (var m in renderer.materials)
            {
                m.shader = Shader.Find(m.shader.name);
            }
        });

        Array.ForEach(go.GetComponentsInChildren<DecalProjector>(), decal =>
        {
            decal.material.shader = Shader.Find(decal.material.shader.name);
        });
#endif

        return go;
    }

    public void SpawnAgents(AgentConfig[] agentConfigs)
    {
        CreateAgentsFromConfigs(agentConfigs);

        if (ActiveAgents.Count > 0)
        {
            SetCurrentActiveAgent(0);
        }
    }

    public void SetupDevAgents()
    {
        var sceneAgents = GameObject.FindGameObjectsWithTag("Player");
        foreach (var agent in sceneAgents)
        {
            var config = agent.GetComponent<AgentController>().Config;
            config.AgentGO = agent;
            ActiveAgents.Add(config);
        }

        if (ActiveAgents.Count == 0)
        {
            string data = null;
#if UNITY_EDITOR
            data = UnityEditor.EditorPrefs.GetString("Simulator/DevelopmentSettings");
#endif
            if (data != null)
            {
                try
                {
                    var json = JSONNode.Parse(data);
                    var createVehicle = json["CreateVehicle"];
                    var vehicleName = json["VehicleName"];
                    if (createVehicle != null && createVehicle.AsBool && vehicleName != null)
                    {
                        using (var db = DatabaseManager.GetConfig(DatabaseManager.GetConnectionString()).Create())
                        {
                            var sql = Sql.Builder.From("vehicles").Where("name = @0", vehicleName.Value);
                            var vehicle = db.SingleOrDefault<VehicleModel>(sql);
                            if (vehicle == null)
                            {
                                Debug.LogError($"Cannot find '{vehicleName.Value}' vehicle in database!");
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

                                    if (manifest.bundleFormat != BundleConfig.Versions[BundleConfig.BundleTypes.Vehicle])
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
                                        throw new Exception($"Failed to load '{bundlePath}' vehicle asset bundle");
                                    }

                                    try
                                    {
                                        var vehicleAssets = vehicleBundle.GetAllAssetNames();
                                        if (vehicleAssets.Length != 1)
                                        {
                                            throw new Exception($"Unsupported '{bundlePath}' vehicle asset bundle, only 1 asset expected");
                                        }

                                        textureBundle?.LoadAllAssets();

                                        if (manifest.fmuName != "")
                                        {
                                            var fmuDirectory = Path.Combine(Application.persistentDataPath, manifest.assetName);
                                            if (platform == "windows")
                                            {
                                                var dll = zip.GetEntry($"{manifest.fmuName}_windows.dll");
                                                if (dll == null)
                                                {
                                                    throw new ArgumentException($"{manifest.fmuName}.dll not found in Zip");
                                                }

                                                using (Stream s = zip.GetInputStream(dll))
                                                {
                                                    byte[] buffer = new byte[4096];
                                                    Directory.CreateDirectory(fmuDirectory);
                                                    var path = Path.Combine(Application.persistentDataPath, manifest.assetName, $"{manifest.fmuName}.dll");
                                                    using (FileStream streamWriter = File.Create(path))
                                                    {
                                                        StreamUtils.Copy(s, streamWriter, buffer);
                                                    }
                                                    vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]).GetComponent<VehicleFMU>().FMUData.Path = path;
                                                }
                                            }
                                            else
                                            {
                                                var dll = zip.GetEntry($"{manifest.fmuName}_linux.so");
                                                if (dll == null)
                                                {
                                                    throw new ArgumentException($"{manifest.fmuName}.so not found in Zip");
                                                }

                                                using (Stream s = zip.GetInputStream(dll))
                                                {
                                                    byte[] buffer = new byte[4096];
                                                    Directory.CreateDirectory(fmuDirectory);
                                                    var path = Path.Combine(Application.persistentDataPath, manifest.assetName, $"{manifest.fmuName}.so");
                                                    using (FileStream streamWriter = File.Create(path))
                                                    {
                                                        StreamUtils.Copy(s, streamWriter, buffer);
                                                    }
                                                    vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]).GetComponent<VehicleFMU>().FMUData.Path = path;
                                                }
                                            }
                                        }

                                        var prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                                        var config = new AgentConfig()
                                        {
                                            Name = vehicle.Name,
                                            Prefab = prefab,
                                            Sensors = vehicle.Sensors,
                                            Connection = json["Connection"].Value,
                                        };
                                        if (!string.IsNullOrEmpty(vehicle.BridgeType))
                                        {
                                            config.Bridge = Simulator.Web.Config.Bridges.Find(bridge => bridge.Name == vehicle.BridgeType);
                                            if (config.Bridge == null)
                                            {
                                                throw new Exception($"Bridge {vehicle.BridgeType} not found");
                                            }
                                        }

                                        var spawn = FindObjectsOfType<SpawnInfo>().OrderBy(s => s.name).FirstOrDefault();
                                        config.Position = spawn != null ? spawn.transform.position : Vector3.zero;
                                        config.Rotation = spawn != null ? spawn.transform.rotation : Quaternion.identity;

                                        SpawnAgent(config);
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
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        else
        {
            var config = ActiveAgents[0];

            var bridgeClient = config.AgentGO.AddComponent<BridgeClient>();
            bridgeClient.Init(new Simulator.Bridge.Ros.RosApolloBridgeFactory());
            bridgeClient.Connect("localhost", 9090);

            var sensorsController = config.AgentGO.GetComponent<SensorsController>();
            if (sensorsController == null)
            {
                sensorsController = config.AgentGO.AddComponent<SensorsController>();
                var agentController = config.AgentGO.GetComponent<AgentController>();
                if (agentController != null)
                    agentController.AgentSensorsController = sensorsController;
            }

            sensorsController.SetupSensors(DefaultSensors.Apollo30);
        }

        ActiveAgents.ForEach(agent => agent.AgentGO.GetComponent<AgentController>().Init());

        SetCurrentActiveAgent(0);
    }

    public void SetCurrentActiveAgent(GameObject agent)
    {
        Debug.Assert(agent != null);
        for (int i = 0; i < ActiveAgents.Count; i++)
        {
            if (ActiveAgents[i].AgentGO == agent)
            {
                SetCurrentActiveAgent(i);
                break;
            }
        }
    }

    public void SetCurrentActiveAgent(int index)
    {
        if (ActiveAgents.Count == 0) return;
        if (index < 0 || index > ActiveAgents.Count - 1) return;
        if (ActiveAgents[index] == null) return;

        CurrentActiveAgent = ActiveAgents[index].AgentGO;
        CurrentActiveAgentController = CurrentActiveAgent.GetComponent<AgentController>();

        foreach (var config in ActiveAgents)
        {
            config.AgentGO.GetComponent<AgentController>().Active = (config.AgentGO == CurrentActiveAgent);
        }
        ActiveAgentChanged(CurrentActiveAgent);
    }

    public void SetNextCurrentActiveAgent()
    {
        var index = GetCurrentActiveAgentIndex();
        index = index < ActiveAgents.Count - 1 ? index + 1 : 0;
        SetCurrentActiveAgent(index);
    }

    public bool GetIsCurrentActiveAgent(GameObject agent)
    {
        return agent == CurrentActiveAgent;
    }

    public int GetCurrentActiveAgentIndex()
    {
        int index = 0;
        for (int i = 0; i < ActiveAgents.Count; i++)
        {
            if (ActiveAgents[i].AgentGO == CurrentActiveAgent)
                index = i;
        }
        return index;
    }

    public float GetDistanceToActiveAgent(Vector3 pos)
    {
        return Vector3.Distance(CurrentActiveAgent.transform.position, pos);
    }

    private void ActiveAgentChanged(GameObject agent)
    {
        AgentChanged?.Invoke(agent);
    }

    private void AgentControllerOnSensorsChanged(AgentController agentController)
    {
        if (agentController == CurrentActiveAgentController)
            ActiveAgentChanged(CurrentActiveAgent);
    }

    public void ResetAgent()
    {
        CurrentActiveAgent?.GetComponent<AgentController>()?.ResetPosition();
    }

    public void DestroyAgent(GameObject go)
    {
        ActiveAgents.RemoveAll(config => config.AgentGO == go);
        var agentController = go.GetComponent<AgentController>();
        if (agentController!= null)
            agentController.SensorsChanged -= AgentControllerOnSensorsChanged;
        Destroy(go);

        if (ActiveAgents.Count == 0)
        {
            SimulatorManager.Instance.CameraManager.SetFreeCameraState();
        }
        else
        {
            SetCurrentActiveAgent(0);
        }
    }

    public void Reset()
    {
        List<AgentConfig> configs = new List<AgentConfig>(ActiveAgents);
        foreach (var config in configs)
        {
            DestroyAgent(config.AgentGO);
        }

        ActiveAgents.Clear();
    }

    private void CreateAgentsFromConfigs(AgentConfig[] agentConfigs)
    {
        var spawns = FindObjectsOfType<SpawnInfo>();
        var positions = spawns.OrderBy(spawn => spawn.name).Select(s => s.transform.position).ToArray();
        var rotations = spawns.OrderBy(spawn => spawn.name).Select(s => s.transform.rotation).ToArray();

        // TODO: In case of spawn point absense on the map
        // we have to do educated guess about default spawn point.
        //
        // The best would be to take meshes tagged as Road and
        // find any point on the surface regarless of the altitude.
        // But for now we use zero.
        int count = positions.Length;
        if (count == 0)
        {
            count = 1;
            positions = new [] { Vector3.zero };
            rotations = new [] { Quaternion.identity };
        }

        var renderers = new List<Renderer>();

        for (int current = 0; current < agentConfigs.Length; current++)
        {
            var config = agentConfigs[current];
            config.Position = positions[current % count];
            config.Rotation = rotations[current % count];

            var agent = SpawnAgent(config);

            // offset current spawn point by agent boundaries
            // in order to place next agent on top of current one
            agent.GetComponentsInChildren(renderers);
            var bounds = new Bounds(config.Position, Vector3.zero);
            renderers.ForEach(renderer => bounds.Encapsulate(renderer.bounds));

            positions[current % count] += Vector3.up * bounds.size.y;
        }
    }

    static byte[] GetFile(ZipFile zip, string entryName)
    {
        var entry = zip.GetEntry(entryName);
        int streamSize = (int)entry.Size;
        byte[] buffer = new byte[streamSize];
        zip.GetInputStream(entry).Read(buffer, 0, streamSize);
        return buffer;
    }
}
