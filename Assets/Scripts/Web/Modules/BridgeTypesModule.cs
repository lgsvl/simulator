/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using System.Linq;
using Simulator.Bridge;

namespace Simulator.Web
{
    public class BridgeTypesModule : NancyModule
    {
        public BridgeTypesModule()
        {
            Get("/bridge-types", _ => BridgePlugins.All.Select(nameAndPlugin => new { Name = nameAndPlugin.Key } ).ToArray());
        }
    }
}
