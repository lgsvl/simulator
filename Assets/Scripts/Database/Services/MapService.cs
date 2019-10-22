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
        public IEnumerable<MapModel> List(string filter, int offset, int count, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    var cleanFilter = $"%{filter.Replace("%", "").Replace("_", "")}%";
                    var filterSql = Sql.Builder
                        .Where(@"(name LIKE @0)", cleanFilter)
                        .OrderBy("id")
                        .Append("LIMIT @0, @1", offset, count);

                    return db.Fetch<MapModel>(filterSql);
                }

                var sql = Sql.Builder
                    .Where("owner = @0 OR owner IS NULL", owner)
                    .OrderBy("id")
                    .Append("LIMIT @0, @1", offset, count);

                return db.Fetch<MapModel>(sql);
            }
        }

        public string GetExistingLocalPath(string url)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("url = @0", url);
                return db.SingleOrDefault<MapModel>(sql)?.LocalPath;
            }
        }

        public int GetCountOfLocal(string localPath)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<int>(Sql.Builder.Select("COUNT(*)").From("maps").Where("localPath = @0", localPath));
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
