/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Data.Services
{
    [MessageType("tb_agv_srvs/SetEffect")]
    public class SetEffect
    {
        public sbyte data;
    }

    [MessageType("tb_agv_srvs/SetEffectResponse")]
    public class SetEffectResponse
    {
        public bool success;
        public string message;
    }

    [MessageType("tb_agv_srvs/SetEffectLED")]
    public class SetEffectLED
    {
        public string data;
    }

    [MessageType("tb_agv_srvs/SetEffectLEDResponse")]
    public class SetEffectLEDResponse
    {
        public bool success;
        public string message;
    }

    [MessageType("tb_agv_srvs/SetStateOfCharge")]
    public class SetStateOfCharge
    {
        public float data;
    }

    [MessageType("tb_agv_srvs/SetStateOfChargeResponse")]
    public class SetStateOfChargeResponse
    {
        public bool success;
        public string message;
    }
}
