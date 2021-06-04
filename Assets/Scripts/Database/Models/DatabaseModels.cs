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
        public string Name { get; set; }
        public string LocalPath { get; set; }

        public string DateAdded { get; set; }
    }

    [TableName("clientSettings")]
    [PrimaryKey("id")]
    public class ClientSettings
    {
        public int id { get; set; }
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
