/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Api.Commands
{
    enum AgentType
    {
        Unknown = 0,
        Ego = 1,
        Npc = 2,
        Pedestrian = 3,
    }

    class AddAgent : ICommand
    {
        public string Name { get { return "simulator/add_agent"; } }

        public void Execute(JSONNode args)
        {
            var sim = Object.FindObjectOfType<SimulatorManager>();
            var api = SimulatorManager.Instance.ApiManager;

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

            if (type == (int)AgentType.Ego)
            {
                var agents = SimulatorManager.Instance.AgentManager;
                GameObject agentGO = null;
                using (var db = Simulator.Database.DatabaseManager.Open())
                {
                    var sql = Sql.Builder.From("vehicles").Where("name = @0", name);
                    var vehicle = db.Single<Simulator.Database.VehicleModel>(sql);
                    var bundlePath = vehicle.LocalPath;
                    var vehicleBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (vehicleBundle == null)
                    {
                        Debug.LogError($"Failed to load vehicle from '{bundlePath}' asset bundle");
                    }
                    try
                    {
                        var vehicleAssets = vehicleBundle.GetAllAssetNames();
                        if (vehicleAssets.Length != 1)
                        {
                            Debug.LogError($"Unsupported vehicle in '{bundlePath}' asset bundle, only 1 asset expected");
                        }
                        
                        var prefab = vehicleBundle.LoadAsset<GameObject>(vehicleAssets[0]);
                        var config = new AgentConfig()
                        {
                            Name = vehicle.Name,
                            Prefab = prefab,
                            Sensors = vehicle.Sensors,
                        };

                        if (vehicle.BridgeType != null)
                        {
                            config.Bridge = Simulator.Web.Config.Bridges.Find(bridge => bridge.Name == vehicle.BridgeType);
                            if (config.Bridge == null)
                            {
                                api.SendError($"Bridge '{vehicle.BridgeType}' not available");
                                return;
                            }
                        }

                        agentGO = agents.SpawnAgent(config);

                        agentGO.transform.position = position;
                        agentGO.transform.rotation = Quaternion.Euler(rotation);

                        var rb = agentGO.GetComponent<Rigidbody>();
                        rb.velocity = velocity;
                        rb.angularVelocity = angular_velocity;
                    }
                    finally
                    {
                        vehicleBundle.Unload(false);
                    }
                }

                var uid = System.Guid.NewGuid().ToString();
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
            }
            else if (type == (int)AgentType.Npc)
            {
                var go = SimulatorManager.Instance.NPCManager.SpawnVehicle(name, position, Quaternion.Euler(rotation));

                var npc = go.GetComponent<NPCController>();
                npc.Control = NPCController.ControlType.Manual;

                var body = go.GetComponent<Rigidbody>();
                body.velocity = velocity;
                body.angularVelocity = angular_velocity;

                var uid = go.name;
                api.Agents.Add(uid, go);
                api.AgentUID.Add(go, uid);
                api.SendResult(new JSONString(go.name));
            }
            else if (type == (int)AgentType.Pedestrian)
            {
                var ped = SimulatorManager.Instance.PedestrianManager.SpawnPedestrianApi(name, position, Quaternion.Euler(rotation));
                if (ped == null)
                {
                    api.SendError($"Unknown '{name}' pedestrian name");
                    return;
                }

                var uid = System.Guid.NewGuid().ToString();
                api.Agents.Add(uid, ped);
                api.AgentUID.Add(ped, uid);

                api.SendResult(new JSONString(uid));
            }
            else
            {
                api.SendError($"Unsupported '{args["type"]}' type");
            }
        }
    }
}
