/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System;

namespace Simulator.Database
{
    [TableName("assets")]
    [PrimaryKey("AssetGuid", AutoIncrement = false)]
    public class AssetModel
    {
        public string AssetGuid { get; set; }
        public string Type { get; set; }
        public string LocalPath { get; set; }
    }

    [TableName("clientSettings")]
    [PrimaryKey("simid", AutoIncrement = false)]
    public class ClientSettings
    {
        public string simid { get; set; }
        public bool onlineStatus { get; set; }
    }

    [TableName("simulations")]
    [PrimaryKey("simid", AutoIncrement = false)]
    public class Simulation
    {
        public string simid { get; set; }
        public string simData { get; set; }
    }
}
