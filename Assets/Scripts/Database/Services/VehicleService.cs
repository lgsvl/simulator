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
        public long Add(Vehicle map) => (long)Database.Insert(map);
        public int Update(Vehicle map) => Database.Update(map);
        public int Delete(long id) => Database.Delete<Vehicle>(id);
    }
}
