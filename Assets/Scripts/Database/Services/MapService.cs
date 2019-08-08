/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using Simulator.Web;
using System;
using System.Collections.Generic;
using System.IO;

namespace Simulator.Database.Services
{
    public class MapService : IMapService
    {
        public IEnumerable<MapModel> List(int page, int count, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("owner = @0 OR owner IS NULL", owner).OrderBy("id");
                return db.Page<MapModel>(page, count, sql).Items;
            }
        }

        public MapModel Get(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                return db.Single<MapModel>(sql);
            }
        }

        public long Add(MapModel map)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(map);
            }
        }

        public int Update(MapModel map)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(map);
            }
        }

        public int Delete(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                return db.Delete<MapModel>(sql);
            }
        }
    }
}
