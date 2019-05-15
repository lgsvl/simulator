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
        public IEnumerable<Vehicle> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<Vehicle>(page, count).Items;
            }
        }

        public Vehicle Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<Vehicle>(id);
            }
        }

        public long Add(Vehicle vehicle)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(vehicle);
            }
        }

        public int Update(Vehicle vehicle)
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
                return db.Delete<Vehicle>(id);
            }
        }
    }
}
