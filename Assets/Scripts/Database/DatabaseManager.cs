/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Mono.Data.Sqlite;
using PetaPoco;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEngine;
using Simulator.Web;
using Simulator.Sensors;

namespace Simulator.Database
{
    public static class DatabaseManager
    {
        static IDatabaseBuildConfiguration DbConfig;

        public static IDatabase Open()
        {
            return DbConfig.Create();
        }

        public static void Init(string connectionString)
        {
            DbConfig = DatabaseConfiguration.Build()
                .UsingConnectionString(connectionString)
                .UsingProvider(new Providers.UnityDatabaseProvider())
                .UsingDefaultMapper<ConventionMapper>(m =>
                {
                    // Vehicle => vehicles
                    m.InflectTableName = (inflector, tn) => inflector.Pluralise(inflector.Uncapitalise(tn));

                    // TimeOfDay => timeOfDay
                    m.InflectColumnName = (inflector, cn) => inflector.Uncapitalise(cn);
                });

            using (var db = new SqliteConnection(connectionString))
            {
                db.Open();
                string[] expectedTables = { "maps", "vehicles", "clusters", "simulations" };
                if (!TablesExist(expectedTables, db))
                {
                    var sql = Resources.Load<TextAsset>("Database/simulator");
                    using (var command = new SqliteCommand(sql.text, db))
                    {
                        command.ExecuteNonQuery();
                    }

                    CreateDefaultDbAssets();
                }
            }
        }

        static void CreateDefaultDbAssets()
        {
            string os;
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                os = "windows";
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                os = "linux";
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                os = "macos";
            }
            else
            {
                return;
            }

            var info = Resources.Load<Utilities.BuildInfo>("BuildInfo");
            if (info == null || info.GitCommit == null || info.DownloadHost == null)
            {
                return;
            }

            using (var db = Open())
            {
                if (info.DownloadEnvironments != null)
                {
                    foreach (var e in info.DownloadEnvironments)
                    {
                        var url = $"https://{info.DownloadHost}/{info.GitCommit}/{os}/environment_{e.ToLowerInvariant()}";
                        var map = new MapModel()
                        {
                            Name = e,
                            Status = "Downloading",
                            Url = url,
                            LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(new Uri(url).AbsolutePath)),
                        };
                        db.Insert(map);
                    }
                }
                if (info.DownloadVehicles != null)
                {
                    foreach (var v in info.DownloadVehicles)
                    {
                        if (v == "Lexus2016RXHybrid")
                        {
                            AddVehicle(db, info, os, v, DefaultSensors.Autoware, " (Autoware)");
                            AddVehicle(db, info, os, v, DefaultSensors.Apollo30, " (Apollo 3.0)");
                            AddVehicle(db, info, os, v, DefaultSensors.Apollo35, " (Apollo 3.5)");
                        }
                        else
                        {
                            AddVehicle(db, info, os, v, DefaultSensors.Apollo30);
                        }
                    }
                }
            }
        }

        static void AddVehicle(IDatabase db, Utilities.BuildInfo info, string os, string name, string sensors, string suffix = null)
        {
            var url = $"https://{info.DownloadHost}/{info.GitCommit}/{os}/vehicle_{name.ToLowerInvariant()}";
            var vehicle = new VehicleModel()
            {
                Name = name + (suffix == null ? string.Empty : suffix),
                Status = "Downloading",
                Url = url,
                LocalPath = Path.Combine(Config.PersistentDataPath, Path.GetFileName(new Uri(url).AbsolutePath)),
                Sensors = sensors,
            };
            db.Insert(vehicle);
        }

        public static List<MapModel> PendingMapDownloads()
        {
            using (var db = Open())
            {
                var sql = Sql.Builder.From("maps").Where("status = @0", "Downloading");
                return db.Page<MapModel>(0, 100, sql).Items;
            }
        }

        public static List<VehicleModel> PendingVehicleDownloads()
        {
            using (var db = Open())
            {
                var sql = Sql.Builder.From("vehicles").Where("status = @0", "Downloading");
                return db.Page<VehicleModel>(0, 100, sql).Items;
            }
        }

        public static bool TablesExist(string[] tableNames, SqliteConnection connection)
        {
            for (int i = 0; i < tableNames.Length; i++)
            {
                if (!TableExists(tableNames[i], connection)) return false;
            }
            return true;
        }

        public static bool TableExists(string tableName, SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.Add("@name", DbType.String).Value = tableName;
                return cmd.ExecuteScalar() != null;
            }
        }
    }
}