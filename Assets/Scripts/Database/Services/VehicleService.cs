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

        public IEnumerable<Vehicle> List(int page, int count)
        {
            Open();
            List<Vehicle> items = Database.Page<Vehicle>(page, count).Items;
            Close();
            return items;
        }

        public Vehicle Get(long id)
        {
            Open();
            Vehicle item = Database.Single<Vehicle>(id);
            Close();
            return item;
        }

        public long Add(Vehicle vehicle)
        {
            Open();
            long l = (long)Database.Insert(vehicle);
            Close();
            return l;
        }

        public int Update(Vehicle vehicle)
        {
            Open();
            int i = Database.Update(vehicle);
            Close();
            return i;
        }

        public int GetCountOfLocal(string localPath)
        {
            Open();
            int i = Database.Single<int>(Sql.Builder.Select("COUNT(*)").From("vehicles").Where("localPath = @0", localPath));
            Close();
            return i;
        }

        public int Delete(long id)
        {
            Open();
            int i = Database.Delete<Vehicle>(id);
            Close();
            return i;
        }
    }
}
