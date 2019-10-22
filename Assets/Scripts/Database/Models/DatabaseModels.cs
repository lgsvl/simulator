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
    [TableName("users")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class UserModel
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string SecretKey { get; set; }
        public string Settings { get; set; }
    }

    [TableName("sessions")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class SessionModel
    {
        public long Id { get; set; }
        public string Cookie { get; set; }
        public string Username { get; set; }
        public DateTime Expire { get; set; }
    }

    [TableName("maps")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class MapModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public string Url { get; set; }
        public string PreviewUrl { get; set; }
        public string LocalPath { get; set; }
        public string Error { get; set; }
    }

    [TableName("vehicles")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class VehicleModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public string Url { get; set; }
        public string BridgeType { get; set; }
        public string PreviewUrl { get; set; }
        public string LocalPath { get; set; }
        public string Sensors { get; set; }
        public string Error { get; set; }
    }

    [TableName("clusters")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class ClusterModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public string Ips { get; set; }
    }

    [TableName("simulations")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class SimulationModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public long? Cluster { get; set; }
        public long? Map { get; set; }
        [PetaPoco.Ignore]
        public ConnectionModel[] Vehicles { get; set; }
        public bool? ApiOnly { get; set; }
        public bool? Interactive { get; set; }
        public bool? Headless { get; set; }
        public DateTime? TimeOfDay { get; set; }
        public float? Rain { get; set; }
        public float? Fog { get; set; }
        public float? Wetness { get; set; }
        public float? Cloudiness { get; set; }
        public int? Seed { get; set; }
        public bool? UseTraffic { get; set; }
        public bool? UsePedestrians { get; set; }
        public bool? UseBicyclists { get; set; }
        public string Error { get; set; }
    }

    [TableName("connections")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class ConnectionModel
    {
        public long Id { get; set; }
        public long Simulation { get; set; }
        public long Vehicle { get; set; }
        public string Connection { get; set; }
    }
}
