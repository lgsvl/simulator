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
using Simulator.Bridge.Ros;
using Simulator.Bridge.Cyber;

namespace Simulator.Database
{
    public static class DatabaseManager
    {
        static IDatabaseBuildConfiguration DbConfig;

        public static IDatabase Open()
        {
            return DbConfig.Create();
        }

        static string GetDatabasePath()
        {
            return Path.Combine(Config.PersistentDataPath, "data.db");
        }

        static string GetBackupPath()
        {
            return Path.Combine(Config.PersistentDataPath, "backup.db");
        }

        public static string GetConnectionString()
        {
            return $"Data Source = {GetDatabasePath()};version=3;";
        }

        static string UncapitaliseInvariant(string word)
        {
            return word.Substring(0, 1).ToLowerInvariant() + word.Substring(1);
        }

        public static IDatabaseBuildConfiguration GetConfig(string connectionString)
        {
            return DatabaseConfiguration.Build()
                .UsingConnectionString(connectionString)
                .UsingProvider(new Providers.UnityDatabaseProvider())
                .UsingDefaultMapper<ConventionMapper>(m =>
                {
                    // Vehicle => vehicles
                    m.InflectTableName = (inflector, tn) => inflector.Pluralise(UncapitaliseInvariant(tn));

                    // TimeOfDay => timeOfDay
                    m.InflectColumnName = (inflector, cn) => UncapitaliseInvariant(cn);
                });
        }

        public static void Init()
        {
            var connectionString = GetConnectionString();
            DbConfig = GetConfig(connectionString);

            using (var db = new SqliteConnection(connectionString))
            {
                try
                {
                    db.Open();
                }
                catch (SqliteException ex)
                {
                    Debug.LogError(ex);
                    Debug.Log("Cannot open database, removing it and replacing with backup");
                    try
                    {
                        File.Copy(GetBackupPath(), GetDatabasePath(), true);
                        db.Open();
                    }
                    catch (Exception backupEx)
                    {
                        Debug.LogError(backupEx);
                        Debug.Log("Cannot open backup, deleting");
                        File.Delete(GetDatabasePath());
                        File.Delete(GetBackupPath());
                        db.Open();
                    }
                }

                CheckForPatches();
            }

            File.Copy(GetDatabasePath(), GetBackupPath(), true);
        }

        static void CheckForPatches()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                string[] expectedTables = { "assets", "clientSettings", "simulations" };
                connection.Open();
                if (!TablesExist(expectedTables, connection))
                {
                    var sql = Resources.Load<TextAsset>("Database/simulator");
                    using (var command = new SqliteCommand(sql.text, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }

            long currentVersion;

            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                using (var command = new SqliteCommand(connection))
                {
                    command.CommandText = "PRAGMA user_version";
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        currentVersion = reader.GetFieldValue<long>(0);
                    }
                }
                connection.Close();
            }

            Debug.Log($"Current Database Version: {currentVersion}");

            for (long i = currentVersion + 1; ; i++)
            {
                TextAsset patch = Resources.Load<TextAsset>($"Database/Patches/{i}");
                if (patch == null)
                {
                    currentVersion = i - 1;
                    break;
                }

                string version = patch.name;
                Debug.Log($"Applying patch {version} to database...");
                using (var connection = new SqliteConnection(GetConnectionString()))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(connection))
                    {
                        command.CommandText = patch.text;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }

            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                using (var command = new SqliteCommand(connection))
                {
                    command.CommandText = $"PRAGMA user_version = {currentVersion};";
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }

            Debug.Log($"Final Database Version: {currentVersion}");
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