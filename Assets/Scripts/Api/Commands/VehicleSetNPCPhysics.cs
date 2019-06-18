/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class VehicleSetNPCPhysics : ICommand
    {
        public string Name { get { return "vehicle/set_npc_physics"; } }

        public void Execute(JSONNode args)
        {
            var api = SimulatorManager.Instance.ApiManager;
            var isPhysicsSimple = args["isPhysicsSimple"].AsBool;
            SimulatorManager.Instance.NPCManager.isSimplePhysics = isPhysicsSimple;
            api.SendResult();
        }
    }
}