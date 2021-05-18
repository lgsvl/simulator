/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public byte Id { get; private set; }

        public CommandAttribute(byte id)
        {
            Id = id;
        }
    }

    public static class Commands
    {
        public class Ready
        {
        }
        
        public class Loaded
        {
        }
        
        public class Run
        {
        }
        
        public class Stop
        {
            public string SimulationId { get; set; }
        }
        
        public class EnvironmentState
        {
            public float Fog { get; set; }
            public float Rain { get; set; }
            public float Wet { get; set; }
            public float Cloud { get; set; }
            public float Damage { get; set; }
            public float TimeOfDay { get; set; }
        }

        public class Ping
        {
            public int Id { get; set; }
        }

        public class Pong
        {
            public int Id { get; set; }
        }
    }
}
