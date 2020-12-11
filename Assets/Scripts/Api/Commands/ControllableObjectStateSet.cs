/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using SimpleJSON;

namespace Simulator.Api.Commands
{
    class ControllableObjectStateSet : ICommand
    {
        public string Name => "controllable/object_state/set";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var manager = SimulatorManager.Instance.ControllableManager;
            var api = ApiManager.Instance;

            if (manager.TryGetControllable(uid, out var controllable))
            {
                var position = args["state"]["transform"]["position"].ReadVector3();
                var rotation = args["state"]["transform"]["rotation"].ReadVector3();
                var velocity = args["state"]["velocity"].ReadVector3();
                var angular_velocity = args["state"]["angular_velocity"].ReadVector3();

                controllable.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));

                var rb = controllable.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = velocity;
                    rb.angularVelocity = angular_velocity;
                }

                api.SendResult(this);
            }
            else
            {
                api.SendError(this, $"Controllable '{uid}' not found");
            }
        }
    }
}
