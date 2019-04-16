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
        [MessageType("tb_agv_srvs/SetEffectLED")]
        public struct SetEffectLED
        {
            public string data;
        }

        [MessageType("tb_agv_srvs/SetEffectLEDResponse")]
        public struct SetEffectLEDResponse
        {
            public bool success;
            public string message;
        }
    }
}
