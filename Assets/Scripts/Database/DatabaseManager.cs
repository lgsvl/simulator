using Database.Providers;
using PetaPoco;
using Mono.Data.Sqlite;

using System;
using System.Data;

using UnityEngine;

namespace Database
{
    public static class DatabaseManager
    {
        static IDatabaseBuildConfiguration DbConfig;

        [ThreadStatic]
        static IDatabase Db;

        public static IDatabase CurrentDb => Db;

        public static IDatabase Open()
        {
            return Db = DbConfig.Create();
        }

        public static void Init(string connectionString)
        {
            DbConfig = DatabaseConfiguration.Build()
                .UsingConnectionString(connectionString)
                .UsingProvider(new UnityDatabaseProvider())
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
                    var textAsset = Resources.Load<TextAsset>("Database/simulator");
                    string sql = textAsset.text;
                    using (var command = new SqliteCommand(sql, db))
                    {
                        command.ExecuteNonQuery();
                    }
                }
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