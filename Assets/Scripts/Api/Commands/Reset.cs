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
    class Reset : ICommand
    {
        public string Name { get { return "simulator/reset"; } }

        public static void Run()
        {
            foreach (var kv in ApiManager.Instance.Agents)
            {
                var obj = kv.Value;
                var sensors = obj.GetComponentsInChildren<SensorBase>();
                
                foreach (var sensor in sensors)
                {
                    var suid = ApiManager.Instance.SensorUID[sensor];
                    ApiManager.Instance.Sensors.Remove(suid);
                    ApiManager.Instance.SensorUID.Remove(sensor);
                }

                // TODO remove ui
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
            }

            ApiManager.Instance.Reset();
        }

        public void Execute(JSONNode args)
        {
            Run();
            ApiManager.Instance.SendResult();
        }
    }
}
