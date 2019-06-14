/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Api.Commands
{
    class RemoveAgent : ICommand
    {
        public string Name { get { return "simulator/agent/remove"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = SimulatorManager.Instance.ApiManager;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var sensors = obj.GetComponentsInChildren<SensorBase>();
                
                foreach (var sensor in sensors)
                {
                    var suid = api.SensorUID[sensor];
                    api.Sensors.Remove(suid);
                    api.SensorUID.Remove(sensor);
                }

                // TODO ui
                SimulatorManager.Instance.AgentManager.DestroyAgent(obj);

                var npc = obj.GetComponent<NPCController>();
                if (npc != null)
                {
                    SimulatorManager.Instance.NPCManager.DespawnVehicle(npc);
                }

                var ped = obj.GetComponent<PedestrianController>();
                if (ped != null)
                {
                    SimulatorManager.Instance.PedestrianManager.DespawnPedestrianApi(ped);
                }

                api.Agents.Remove(uid);
                api.AgentUID.Remove(obj);
                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
