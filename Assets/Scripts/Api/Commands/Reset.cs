/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using Simulator.Sensors;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{
    class Reset : ICommand, IDistributedObject
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
