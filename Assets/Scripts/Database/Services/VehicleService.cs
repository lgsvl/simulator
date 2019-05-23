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
        public IEnumerable<VehicleModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<VehicleModel>(page, count).Items;
            }
        }

        public VehicleModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<VehicleModel>(id);
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

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Delete<VehicleModel>(id);
            }
        }
    }
}
