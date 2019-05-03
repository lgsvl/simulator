using PetaPoco.Providers;
using System.Data.Common;

namespace Database.Providers
{
    public class UnityDatabaseProvider : SQLiteDatabaseProvider
    {
        public override DbProviderFactory GetFactory()
            => GetFactory("Mono.Data.Sqlite.SqliteFactory, Mono.Data.Sqlite");
    }
}