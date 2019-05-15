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
    public class MapService : IMapService
    {
        IDatabase Database;

        public void Open() => Database = DatabaseManager.Open();
        public void Close() => Database.Dispose();

        public IEnumerable<Map> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return Database.Page<Map>(page, count).Items;
            }
        }

        public Map Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return Database.Single<Map>(id);
            }
        }

        public long Add(Map map)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)Database.Insert(map);
            }
        }

        public int Update(Map map)
        {
            using (var db = DatabaseManager.Open())
            {
                return Database.Update(map);
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return Database.Delete<Map>(id);
            }
        }
    }
}
