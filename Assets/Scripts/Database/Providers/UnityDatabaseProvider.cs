using System.Data.Common;

using PetaPoco.Providers;

namespace Database.Providers
{
    public class UnityDatabaseProvider : SQLiteDatabaseProvider
    {
        public override DbProviderFactory GetFactory()
            => GetFactory("Mono.Data.Sqlite.SqliteFactory, Mono.Data.Sqlite");
    }
}