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
    public class SimulationService : ISimulationService
    {
        IDatabase Database;

        public void Open() => Database = DatabaseManager.Open();
        public void Close() => Database.Dispose();

        public IEnumerable<Simulation> List(int page, int count) => Database.Page<Simulation>(page, count).Items;
        public Simulation Get(long id) => Database.Single<Simulation>(id);
        public long Add(Simulation vehicle) => (long)Database.Insert(vehicle);
        public int Update(Simulation vehicle) => Database.Update(vehicle);
        public int Delete(long id) => Database.Delete<Vehicle>(id);

        public string GetActualStatus(Simulation simulation)
        {
            if (Loader.Instance.CurrentSimulation != null && simulation.Id == Loader.Instance.CurrentSimulation.Id)
            {
                return "Running";
            }

            var sql = Sql.Builder.Select("COUNT(*)").From("maps").Where("id = @0", simulation.Map).Where("status = @0", "Valid");
            int count = Database.Single<int>(sql);
            if (count != 1)
            {
                return "Invalid";
            }

            if (string.IsNullOrEmpty(simulation.Vehicles))
            {
                return "Valid";
            }

            sql = Sql.Builder.Select("COUNT(*)").From("vehicles").Where("id IN (@0)", simulation.Vehicles).Where("status = @0", "Valid");
            count = Database.Single<int>(sql);
            if (count != simulation.Vehicles.Split(',').Length)
            {
                return "Invalid";
            }

            return "Valid";
        }

        // TODO: these probably should be in different service
        public Simulation GetCurrent() => Loader.Instance.CurrentSimulation;
        public void Start(Simulation simulation) => Loader.StartAsync(simulation);
        public void Stop() => Loader.StopAsync();
    }
}
