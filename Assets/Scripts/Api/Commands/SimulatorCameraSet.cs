/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using System;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class SimulatorCameraSet : IDistributedCommand
    {
        public string Name => "simulator/camera/set";

        public void Execute(JSONNode args)
        {
            var camManager = SimulatorManager.Instance.CameraManager;
            var api = ApiManager.Instance;

            if (camManager == null)
            {
                api.SendError(this, "Camera Manager not found. Is the scene loaded?");
                return;
            }

            var position = args["transform"]["position"].ReadVector3();
            var rotation = args["transform"]["rotation"].ReadVector3();

            camManager.SetFreeCameraState(position, rotation);
            api.SendResult(this);
        }
    }
}
