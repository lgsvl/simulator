/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System.Collections.Generic;
using System;
using System.IO;

namespace Simulator
{
    public static class BundleConfig
    {
        public enum BundleTypes
        {
            Vehicle,
            Environment,
            Sensor,
            Controllable,
            NPC,
            Bridge,
        }

        public static Dictionary<BundleTypes, string> Versions = new Dictionary<BundleTypes, string>()
        {
            [BundleTypes.Vehicle]     = "com.svlsimulator.3",
            [BundleTypes.Environment] = "com.svlsimulator.2",
            [BundleTypes.Sensor]      = "com.svlsimulator.3",
            [BundleTypes.Controllable]= "com.svlsimulator.0",
            [BundleTypes.NPC]         = "com.svlsimulator.0",
            [BundleTypes.Bridge]      = "com.svlsimulator.0",
        };

        public static string singularOf(BundleTypes type) => Enum.GetName(typeof(BundleTypes), type);
        public static string pluralOf(BundleTypes type) => Enum.GetName(typeof(BundleTypes), type) + "s";
        public static string ExternalBase = Path.Combine("Assets", "External");
    }
}
