/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;

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
                var agents = ROSAgentManager.Instance;
                foreach (var agent in agents.activeAgents)
                {
                    agent.Agent.GetComponent<AgentSetup>().FollowCamera.gameObject.SetActive(false);
                }

                var agentType = agents.agentPrefabs.Find(prefab => prefab.name == name);
                var connector = new RosBridgeConnector(agentType);
                agents.Add(connector);
                sim.SpawnVehicle(position, Quaternion.Euler(rotation), connector, null);

                agents.SetCurrentActiveAgent(connector);

                var setup = connector.Agent.GetComponent<AgentSetup>();
                setup.FollowCamera.gameObject.SetActive(true);

                var body = connector.Agent.GetComponent<Rigidbody>();
                body.velocity = velocity;
                body.angularVelocity = angular_velocity;

                SegmentationManager.Instance.SetupVehicle(connector.Agent);

                var uid = System.Guid.NewGuid().ToString();
                ApiManager.Instance.Agents.Add(uid, connector.Agent);
                ApiManager.Instance.AgentUID.Add(connector.Agent, uid);

                foreach (var sensor in setup.GetSensors())
                {
                    var sensor_uid = System.Guid.NewGuid().ToString();
                    ApiManager.Instance.SensorUID.Add(sensor, sensor_uid);
                    ApiManager.Instance.Sensors.Add(sensor_uid, sensor);
                }

                ApiManager.Instance.SendResult(new JSONString(uid));
            }
            else if (type == (int)AgentType.Npc)
            {
                var go = NPCManager.Instance.SpawnVehicle(name, position, Quaternion.Euler(rotation));

                var npc = go.GetComponent<NPCControllerComponent>();
                npc.Control = NPCControllerComponent.ControlType.Manual;

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
                var ped = PedestrianManager.Instance.SpawnPedestrianApi(name, position, Quaternion.Euler(rotation));
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
