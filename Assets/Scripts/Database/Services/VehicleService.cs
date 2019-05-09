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
        IDatabase Database;

        public void Open() => Database = DatabaseManager.Open();
        public void Close() => Database.Dispose();

        public IEnumerable<Vehicle> List(int page, int count) => Database.Page<Vehicle>(page, count).Items;
        public Vehicle Get(long id) => Database.Single<Vehicle>(id);
        public long Add(Vehicle vehicle) => (long)Database.Insert(vehicle);
        public int Update(Vehicle vehicle) => Database.Update(vehicle);
        public int Delete(long id) => Database.Delete<Vehicle>(id);
    }
}
