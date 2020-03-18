/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;

namespace Simulator.Api.Commands
{
    class ControllableObjectStateGet : ICommand
    {
        public string Name => "controllable/object_state/get";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var api = ApiManager.Instance;
            var controlManager = SimulatorManager.Instance.ControllableManager;

            if (api.Controllables.TryGetValue(uid, out var controllable))
            {
                var result = new JSONObject();

                var transform = new JSONObject();
                transform.Add("position", controllable.transform.position);
                transform.Add("rotation", controllable.transform.rotation.eulerAngles);
                result.Add("transform", transform);

                var rb = controllable.gameObject.GetComponent<Rigidbody>();
                result.Add("velocity", rb != null ? rb.velocity : Vector3.zero);
                result.Add("angular_velocity", rb != null ? rb.angularVelocity : Vector3.zero);

                api.SendResult(this, result);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
