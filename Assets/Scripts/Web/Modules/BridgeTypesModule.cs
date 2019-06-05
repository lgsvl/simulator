/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;

namespace Simulator.Web
{
    public class BridgeTypesModule : NancyModule
    {
        public BridgeTypesModule()
        {
            Get("/bridge-types", _ => Config.Bridges);
        }
    }
}
