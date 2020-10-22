/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class GetLayers : ICommand
    {
        public string Name => "simulator/layers/get";

        public void Execute(JSONNode args)
        {
            JSONObject j = new JSONObject();
            for (int i = 0; i < 32; i++)
            {
                var layerInfo = LayerMask.LayerToName(i);
                if (layerInfo != "")
                {
                    j.Add(layerInfo, i);
                }
            }
            ApiManager.Instance.SendResult(this, j);
        }
    }
}
