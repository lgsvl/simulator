/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;
using Simulator.Controllable;
using Simulator.Network.Core.Identification;

namespace Simulator.Api.Commands
{
    class ControllableAdd : IDistributedCommand
    {
        public string Name => "simulator/controllable_add";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            var controlManager = SimulatorManager.Instance.ControllableManager;

            var name = args["name"].Value;
            var position = args["state"]["transform"]["position"].ReadVector3();
            var rotation = args["state"]["transform"]["rotation"].ReadVector3();
            var velocity = args["state"]["velocity"].ReadVector3();
            var angular_velocity = args["state"]["angular_velocity"].ReadVector3();

            Web.Config.Controllables.TryGetValue(name, out IControllable prefab);
            if (prefab == null)
            {
                api.SendError(this, $"Unknown '{name}' controllable prefab");
                return;
            }

            string uid;
            var argsUid = args["uid"];
            if (argsUid == null)
            {
                uid = System.Guid.NewGuid().ToString();
                // Add uid key to arguments, as it will be distributed to the clients' simulations
                if (Loader.Instance.Network.IsMaster)
                    args.Add("uid", uid);
            }
            else
                uid = argsUid.Value;

            var controllable = controlManager.SpawnControllable(prefab.gameObject, uid, position,
                Quaternion.Euler(rotation), velocity, angular_velocity);
            if (controllable == null)
            {
                api.SendError(this, $"Failed to spawn '{name}' controllable");
                return;
            }

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
            j.Add("uid", new JSONString(controllable.UID));
            j.Add("position", controllable.transform.position);
            j.Add("rotation", controllable.transform.rotation.eulerAngles);
            j.Add("type", new JSONString(controllable.ControlType));
            j.Add("valid_actions", validActions);
            j.Add("default_control_policy", new JSONString(controllable.DefaultControlPolicy));

            api.SendResult(this, j);
        }
    }
}
