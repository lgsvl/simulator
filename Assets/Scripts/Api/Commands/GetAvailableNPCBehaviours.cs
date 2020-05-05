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
    class GetAvailableNPCBehaviours : ICommand
    {
        public string Name => "simulator/npc/available_behaviours";

        public void Execute(JSONNode args)
        {
            var data = new JSONArray();
            var api = ApiManager.Instance;
            foreach (var entry in Simulator.Web.Config.NPCBehaviours)
            {
                var adata = new JSONObject();
                adata["name"] = entry.Key;
                data.Add(adata);
            }

            api.SendResult(this, data);
        }
    }
}
