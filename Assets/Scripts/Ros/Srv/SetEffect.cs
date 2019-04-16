/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


namespace Ros
{
    namespace Srv
    {
        [MessageType("tb_agv_srvs/SetEffect")]
        public struct SetEffect
        {
            public sbyte data;
        }

        [MessageType("tb_agv_srvs/SetEffectResponse")]
        public struct SetEffectResponse
        {
            public bool success;
            public string message;
        }
    }
}
