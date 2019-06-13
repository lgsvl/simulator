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
            if (sim == null)
            {
                ApiManager.Instance.SendError("SimulatorManager not found! Is scene loaded?");
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
                var agents = SimulatorManager.Instance.agentManager;
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
                        agentGO = agents.SpawnAgent(new AgentConfig()
                        {
                            Name = vehicle.Name,
                            Prefab = prefab,
                            Sensors = vehicle.Sensors,
                            Position = position,
                            Rotation = Quaternion.Euler(rotation),
                            Velocity = velocity,
                            Angular = angular_velocity,
                        });
                    }
                    finally
                    {
                        vehicleBundle.Unload(false);
                    }
                }
                
                var uid = System.Guid.NewGuid().ToString();
                Debug.Assert(agentGO != null);
                ApiManager.Instance.Agents.Add(uid, agentGO);
                ApiManager.Instance.AgentUID.Add(agentGO, uid);

                var sensors = agentGO.GetComponentsInChildren<SensorBase>();

                foreach (var sensor in sensors)
                {
                    var sensor_uid = System.Guid.NewGuid().ToString();
                    ApiManager.Instance.SensorUID.Add(sensor, sensor_uid);
                    ApiManager.Instance.Sensors.Add(sensor_uid, sensor);
                }

                ApiManager.Instance.SendResult(new JSONString(uid));
            }
            else if (type == (int)AgentType.Npc)
            {
                var go = SimulatorManager.Instance.npcManager.SpawnVehicle(name, position, Quaternion.Euler(rotation));

                var npc = go.GetComponent<NPCController>();
                npc.Control = NPCController.ControlType.Manual;

                var body = go.GetComponent<Rigidbody>();
                body.velocity = velocity;
                body.angularVelocity = angular_velocity;

                var uid = go.name;
                ApiManager.Instance.Agents.Add(uid, go);
                ApiManager.Instance.AgentUID.Add(go, uid);
                ApiManager.Instance.SendResult(new JSONString(go.name));
            }
            else if (type == (int)AgentType.Pedestrian)
            {
                var ped = SimulatorManager.Instance.pedestrianManager.SpawnPedestrianApi(name, position, Quaternion.Euler(rotation));
                if (ped == null)
                {
                    ApiManager.Instance.SendError($"Unknown '{name}' pedestrian name");
                    return;
                }

                var uid = System.Guid.NewGuid().ToString();
                ApiManager.Instance.Agents.Add(uid, ped);
                ApiManager.Instance.AgentUID.Add(ped, uid);

                ApiManager.Instance.SendResult(new JSONString(uid));
            }
            else
            {
                ApiManager.Instance.SendError($"Unsupported '{args["type"]}' type");
            }
        }
    }
}
