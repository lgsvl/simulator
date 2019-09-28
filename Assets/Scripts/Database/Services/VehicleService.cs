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
    public class VehicleService : IVehicleService
    {
        public IEnumerable<VehicleModel> List(string filter, int offset, int count, string owner)
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

                    return db.Fetch<VehicleModel>();
                }

                var sql = Sql.Builder
                    .Where("owner = @0 OR owner IS NULL", owner)
                    .OrderBy("id")
                    .Append("LIMIT @0, @1", offset, count);

                return db.Fetch<VehicleModel>(sql);
            }
        }

        public VehicleModel Get(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                return db.Single<VehicleModel>(sql);
            }
        }

        public long Add(VehicleModel vehicle)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(vehicle);
            }
        }

        public int Update(VehicleModel vehicle)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(vehicle);
            }
        }

        public int GetCountOfLocal(string localPath)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<int>(Sql.Builder.Select("COUNT(*)").From("vehicles").Where("localPath = @0", localPath));
            }
        }

        public int GetCountOfUrl(string url)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<int>(Sql.Builder.Select("COUNT(*)").From("vehicles").Where("url = @0", url));
            }
        }

        public List<VehicleModel> GetAllMatchingUrl(string url)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Fetch<VehicleModel>(Sql.Builder.From("vehicles").Where("url = @0", url));
            }
        }

        public void SetStatusForPath(string status, string path)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Set("status = @0", status).Where("localPath = @0", path);
                db.Update<VehicleModel>(sql);
            }
        }

        public int Delete(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                return db.Delete<VehicleModel>(sql);
            }
        }
    }
}
