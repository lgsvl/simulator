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
            Open();
            List<Map> items = Database.Page<Map>(page, count).Items;
            Close();
            return items;
        }

        public Map Get(long id)
        {
            Open();
            Map map = Database.Single<Map>(id);
            Close();
            return map;
        }

        public long Add(Map map)
        {
            Open();
            long l = (long)Database.Insert(map);
            Close();
            return l;
        }

        public int Update(Map map)
        {
            Open();
            int i = Database.Update(map);
            Close();
            return i;
        }

        public int Delete(long id)
        {
            Open();
            int i = Database.Delete<Map>(id);
            Close();
            return i;
        }
    }
}
