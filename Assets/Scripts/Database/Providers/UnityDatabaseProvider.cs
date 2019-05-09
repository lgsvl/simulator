/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco.Providers;
using System.Data.Common;

namespace Simulator.Database.Providers
{
    public class UnityDatabaseProvider : SQLiteDatabaseProvider
    {
        public override DbProviderFactory GetFactory()
            => GetFactory("Mono.Data.Sqlite.SqliteFactory, Mono.Data.Sqlite");
    }
}
