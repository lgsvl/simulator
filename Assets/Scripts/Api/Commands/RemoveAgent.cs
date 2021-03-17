/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{

    class RemoveAgent : IDistributedCommand
    {
        public string Name => "simulator/agent/remove";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var sensors = obj.GetComponentsInChildren<SensorBase>();
                
                if (SimulatorManager.InstanceAvailable)
                    foreach (var sensor in sensors)
                        SimulatorManager.Instance.Sensors.UnregisterSensor(sensor);

                SimulatorManager.Instance.AgentManager.DestroyAgent(obj);

                var npc = obj.GetComponent<NPCController>();
                if (npc != null)
                {
                    SimulatorManager.Instance.NPCManager.DestroyNPC(npc);
                }

                var ped = obj.GetComponent<PedestrianController>();
                if (ped != null)
                {
                    SimulatorManager.Instance.PedestrianManager.DespawnPedestrianApi(ped);
                }

                api.Agents.Remove(uid);
                api.AgentUID.Remove(obj);
                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Agent '{uid}' not found");
            }
        }
    }
}
