/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using Simulator.Controllable;
using System.Linq;

namespace Simulator.Api.Commands
{
    class ControllableGetAll : ICommand
    {
        public string Name => "controllable/get/all";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var controlType = args["control_type"].Value;

            var controllables = api.Controllables.Values.ToList();
            if (!string.IsNullOrEmpty(controlType))
            {
                controllables = controllables.FindAll(c => c.ControlType == controlType);
            }

            JSONArray result = new JSONArray();

            foreach (var controllable in controllables)
            {
                if (api.ControllablesUID.TryGetValue(controllable, out string uid))
                {
                    JSONArray validActions = new JSONArray();
                    if (controllable.ValidStates != null)
                    {
                        foreach (var state in controllable.ValidStates)
                        {
                            validActions.Add(state);
                        }
                    }

                    if (controllable.ValidActions != null)
                    {
                        foreach (var action in controllable.ValidActions)
                        {
                            validActions.Add(action);
                        }
                    }

                    JSONObject j = new JSONObject();
                    j.Add("uid", uid);
                    j.Add("position", controllable.transform.position);
                    j.Add("rotation", controllable.transform.rotation.eulerAngles);
                    j.Add("type", controllable.ControlType);
                    j.Add("valid_actions", validActions);
                    j.Add("default_control_policy", controllable.DefaultControlPolicy);
                    result.Add(j);
                }
            }

            api.SendResult(this, result);
        }
    }
}
