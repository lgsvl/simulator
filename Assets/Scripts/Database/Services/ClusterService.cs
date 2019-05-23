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
        public IEnumerable<ClusterModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<ClusterModel>(page, count).Items;
            }
        }

        public ClusterModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<ClusterModel>(id);
            }
        }

        public long Add(ClusterModel cluster)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(cluster);
            }
        }

        public int Update(ClusterModel cluster)
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
                return db.Delete<ClusterModel>(id);
            }
        }
    }
}
