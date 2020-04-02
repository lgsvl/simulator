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
        public class Info
        {
            public string Version { get; set; }
            public string UnityVersion { get; set; }
            public string OperatingSystem { get; set; }
        }

        public struct LoadAgent
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Bridge { get; set; }
            public string Connection { get; set; }
            public string Sensors { get; set; }
        }

        public class Load
        {
            public bool UseSeed { get; set; }
            public int Seed { get; set; }
            public string Name { get; set; }
            public string MapName { get; set; }
            public string MapUrl { get; set; }
            public bool ApiOnly { get; set; }
            public bool Headless { get; set; }
            public bool Interactive { get; set; }
            public string TimeOfDay { get; set; }
            public float Rain { get; set; }
            public float Fog { get; set; }
            public float Wetness { get; set; }
            public float Cloudiness { get; set; }
            public LoadAgent[] Agents { get; set; }
            public bool UseTraffic { get; set; }
            public bool UsePedestrians { get; set; }
        }

        public class LoadResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class Run
        {
        }
        
        public class Stop
        {
        }
        
        public class EnvironmentState
        {
            public float Fog { get; set; }
            public float Rain { get; set; }
            public float Wet { get; set; }
            public float Cloud { get; set; }
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
