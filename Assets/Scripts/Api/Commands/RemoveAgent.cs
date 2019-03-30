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
    class RemoveAgent : ICommand
    {
        public string Name { get { return "simulator/remove_agent"; } }

        public void Execute(string client, JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var setup = obj.GetComponent<AgentSetup>();
                if (setup != null)
                {
                    var sensors = setup.GetSensors();
                    foreach (var sensor in sensors)
                    {
                        var suid = ApiManager.Instance.SensorUID[sensor];
                        ApiManager.Instance.Sensors.Remove(suid);
                        ApiManager.Instance.SensorUID.Remove(sensor);
                    }

                    SimulatorManager.Instance.DespawnVehicle(setup.Connector);
                    ROSAgentManager.Instance.RemoveVehicleObject(obj);
                    Object.Destroy(obj);
                }

                var npc = obj.GetComponent<NPCControllerComponent>();
                if (npc != null)
                {
                    NPCManager.Instance.DespawnVehicle(npc);
                }

                var ped = obj.GetComponent<PedestrianComponent>();
                if (ped != null)
                {
                    PedestrianManager.Instance.DespawnPedestrianApi(ped);
                }

                ApiManager.Instance.Agents.Remove(uid);
                ApiManager.Instance.AgentUID.Remove(obj);
                ApiManager.Instance.SendResult(client, JSONNull.CreateOrGet());
            }
            else
            {
                ApiManager.Instance.SendError(client, $"Agent '{uid}' not found");
            }
        }
    }
}
