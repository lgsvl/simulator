/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Bridge.Ros.Services
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

    [MessageType("tb_agv_srvs/SetStateOfCharge")]
    public struct SetStateOfCharge
    {
        public float data;
    }

    [MessageType("tb_agv_srvs/SetStateOfChargeResponse")]
    public struct SetStateOfChargeResponse
    {
        public bool success;
        public string message;
    }
}
