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
        public static IDatabase db;

        public static void Init(string connectionString)
        {
            if (db != null) return;

            db = DatabaseConfiguration.Build()
                .UsingConnectionString(connectionString)
                .UsingProvider(new UnityDatabaseProvider())
                .UsingDefaultMapper<ConventionMapper>(m =>
                {
                    // Vehicle => vehicles
                    m.InflectTableName = (inflector, tn) => inflector.Pluralise(inflector.Uncapitalise(tn));

                    // TimeOfDay => timeOfDay
                    m.InflectColumnName = (inflector, cn) => inflector.Uncapitalise(cn);
                })
                .Create();

            using (var m_dbConnection = new SqliteConnection(connectionString))
            {
                m_dbConnection.Open();
                string[] expectedTables = { "maps", "vehicles", "clusters", "simulations" };
                if (!TablesExist(expectedTables, m_dbConnection))
                {
                    var textAsset = Resources.Load<TextAsset>("Database/simulator");
                    string sql = textAsset.text;
                    using (var command = new SqliteCommand(sql, m_dbConnection))
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

        public static bool TableExists(String tableName, SqliteConnection connection)
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