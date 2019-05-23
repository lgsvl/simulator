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
        public IEnumerable<MapModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<MapModel>(page, count).Items;
            }
        }

        public MapModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<MapModel>(id);
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

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Delete<MapModel>(id);
            }
        }
    }
}
