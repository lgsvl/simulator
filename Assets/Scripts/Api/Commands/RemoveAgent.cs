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

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                var sensors = obj.GetComponentsInChildren<SensorBase>();
                
                foreach (var sensor in sensors)
                {
                    var suid = ApiManager.Instance.SensorUID[sensor];
                    ApiManager.Instance.Sensors.Remove(suid);
                    ApiManager.Instance.SensorUID.Remove(sensor);
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

                ApiManager.Instance.Agents.Remove(uid);
                ApiManager.Instance.AgentUID.Remove(obj);
                ApiManager.Instance.SendResult();
            }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
