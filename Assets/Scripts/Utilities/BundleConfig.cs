/**
 * Copyright (c) 2019 LG Electronics, Inc.
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
            NPC
        }

        public static Dictionary<BundleTypes, int> Versions = new Dictionary<BundleTypes, int>(){
            [BundleTypes.Vehicle]     = 3,
            [BundleTypes.Environment] = 2,
            [BundleTypes.Sensor]      = 2,
            [BundleTypes.Controllable]= 0,
            [BundleTypes.NPC]         = 0,
        };

        public static string pluralOf(BundleTypes type) => Enum.GetName(typeof(BundleConfig.BundleTypes), type) + "s";
        public static string ExternalBase = Path.Combine("Assets", "External");
    }
}
