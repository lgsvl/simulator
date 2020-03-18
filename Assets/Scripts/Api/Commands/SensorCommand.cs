/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Net;
using SimpleJSON;
using Simulator.Network.Core;

namespace Simulator.Api.Commands
{
    public abstract class SensorCommand : IDelegatedCommand
    {
        public abstract string Name { get; }

        public abstract void Execute(JSONNode args);

        public IPEndPoint TargetNodeEndPoint(JSONNode args)
        {
            var network = Loader.Instance.Network;
            if (!network.IsMaster)
                return null;
            var uid = args["uid"].Value;
            return SimulatorManager.InstanceAvailable ? SimulatorManager.Instance.Sensors.GetHostEndPoint(uid) : null;
        }
    }
}
