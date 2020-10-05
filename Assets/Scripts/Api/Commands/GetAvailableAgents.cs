/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System;
using Simulator.Map;

namespace Simulator.Api.Commands
{
    class GetAvailableAgents : ICommand
    {
        public string Name => "simulator/available_agents";

        public void Execute(JSONNode args)
        {
            var data = new JSONArray();
            var api = ApiManager.Instance;
            foreach (var agent in Simulator.Web.Config.NPCVehicles)
            {
                var adata = new JSONObject();
                adata["name"] = agent.Key;
                adata["type"] = "NPC";
                adata["NPCType"] = Enum.GetName(typeof(NPCSizeType), agent.Value.NPCType);
                adata["loaded"] = agent.Value.Prefab != null;
                adata["AssetGuid"] = agent.Value.AssetGuid;
                data.Add(adata);
            }

            api.SendResult(this, data);
        }
    }
}
