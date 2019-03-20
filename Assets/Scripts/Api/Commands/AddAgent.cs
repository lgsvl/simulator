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

        public void Execute(string client, JSONNode args)
        {
            var sim = Object.FindObjectOfType<SimulatorManager>();
            if (sim == null)
            {
                ApiManager.Instance.SendError(client, "SimulatorManager not found! Is scene loaded?");
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

                var setup = connector.Agent.GetComponent<AgentSetup>();
                setup.FollowCamera.gameObject.SetActive(true);

                var body = connector.Agent.GetComponent<Rigidbody>();
                body.velocity = body.transform.InverseTransformVector(velocity);
                body.angularVelocity = angular_velocity;

                var uid = System.Guid.NewGuid().ToString();
                ApiManager.Instance.Agents.Add(uid, connector.Agent);

                foreach (var sensor in setup.GetSensors())
                {
                    var sensor_uid = System.Guid.NewGuid().ToString();
                    ApiManager.Instance.SensorUID.Add(sensor, sensor_uid);
                    ApiManager.Instance.Sensors.Add(sensor_uid, sensor);
                }

                ApiManager.Instance.SendResult(client, new JSONString(uid));
            }
            else if (type == (int)AgentType.Npc)
            {
                var go = NPCManager.Instance.SpawnVehicle(name, position, Quaternion.Euler(rotation));

                var npc = go.GetComponent<NPCControllerComponent>();
                npc.Control = NPCControllerComponent.ControlType.Manual;

                var body = go.GetComponent<Rigidbody>();
                body.velocity = body.transform.InverseTransformVector(velocity);
                body.angularVelocity = angular_velocity;

                var uid = go.name;
                ApiManager.Instance.Agents.Add(uid, go);
                ApiManager.Instance.SendResult(client, new JSONString(go.name));
            }
            else if (type == (int)AgentType.Pedestrian)
            {
                ApiManager.Instance.SendError(client, $"PEDESTRIAN type is not implemented yet");
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Unsupported '{args["type"]}' type");
            }
        }
    }
}
