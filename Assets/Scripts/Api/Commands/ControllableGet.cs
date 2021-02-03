/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
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
    using Utilities;

    class ControllableGet : ICommand
    {
        public string Name => "controllable/get";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            if (TryParseUid(args, out var result))
            {
                api.SendResult(this, result);
                return;
            }

            var manager = SimulatorManager.Instance.ControllableManager;
            var position = args["position"].ReadVector3();
            var controlType = args["control_type"].Value;

            var controllables = manager.Controllables;
            if (!string.IsNullOrEmpty(controlType))
            {
                controllables = controllables.FindAll(c => c.ControlType == controlType);
            }

            IControllable controllable = GetClosestControllable(position, controllables);
            if (controllable == null)
            {
                api.SendError(this, $"Controllable object not found with '{position}'");
            }

            api.SendResult(this, GetResult(controllable));
        }

        private bool TryParseUid(JSONNode args, out JSONNode result)
        {
            if (!args.HasKey("uid"))
            {
                result = null;
                return false;
            }

            var uid = args["uid"];
            var manager = SimulatorManager.Instance.ControllableManager;
            result = manager.TryGetControllable(uid, out var controllable) ? GetResult(controllable) : new JSONString($"Controllable object not found with uid '{uid}'");
            return true;
        }

        private JSONNode GetResult(IControllable controllable)
        {
            var uid = controllable.UID;

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
            j.Add("default_control_policy", Utility.SerializeControlPolicy(controllable.DefaultControlPolicy));
            return j;
        }

        private IControllable GetClosestControllable(Vector3 targetPos, List<IControllable> controllables)
        {
            IControllable controllable = null;
            float minDist = Mathf.Infinity;
            foreach (var c in controllables)
            {
                float dist = Vector3.Distance(c.transform.position, targetPos);
                if (dist < minDist)
                {
                    controllable = c;
                    minDist = dist;
                }
            }

            return controllable;
        }
    }
}
