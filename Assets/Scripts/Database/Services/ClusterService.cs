/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public class ClusterService : IClusterService
    {
        IDatabase Database;

        public void Open() => Database = DatabaseManager.Open();
        public void Close() => Database.Dispose();

        public IEnumerable<Cluster> List(int page, int count) => Database.Page<Cluster>(page, count).Items;
        public Cluster Get(long id) => Database.Single<Cluster>(id);
        public long Add(Cluster cluster) => (long)Database.Insert(cluster);
        public int Update(Cluster cluster) => Database.Update(cluster);
        public int Delete(long id) => Database.Delete<Cluster>(id);
    }
}
