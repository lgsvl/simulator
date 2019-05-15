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
        public IEnumerable<Cluster> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<Cluster>(page, count).Items;
            }
        }

        public Cluster Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<Cluster>(id);
            }
        }

        public long Add(Cluster cluster)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(cluster);
            }
        }

        public int Update(Cluster cluster)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(cluster);
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Delete<Cluster>(id);
            }
        }
    }
}
