/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using Simulator.Sensors;

namespace Simulator.Api.Commands
{
    class Reset : ICommand
    {
        public string Name => "simulator/reset";

        public static void Run()
        {
            var api = ApiManager.Instance;
            foreach (var kv in api.Agents)
            {
                var obj = kv.Value;
                var sensors = obj.GetComponentsInChildren<SensorBase>();

                foreach (var sensor in sensors)
                {
                    var suid = api.SensorUID[sensor];
                    api.Sensors.Remove(suid);
                    api.SensorUID.Remove(sensor);
                }

                var sim = SimulatorManager.Instance;

                if (obj.GetComponent<VehicleController>() != null)
                {
                    sim.AgentManager.DestroyAgent(obj);
                }

                var npc = obj.GetComponent<NPCController>();
                if (npc != null)
                {
                    sim.NPCManager.DespawnVehicle(npc);
                }

                var ped = obj.GetComponent<PedestrianController>();
                if (ped != null)
                {
                    sim.PedestrianManager.DespawnPedestrianApi(ped);
                }
            }

            api.Reset();
            SIM.LogAPI(SIM.API.SimulationReset);
        }

        public void Execute(JSONNode args)
        {
            Run();
            ApiManager.Instance.SendResult();
        }
    }
}
