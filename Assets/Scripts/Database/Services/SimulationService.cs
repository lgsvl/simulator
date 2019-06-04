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
        public IEnumerable<SimulationModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<SimulationModel>(page, count).Items;
            }
        }

        public SimulationModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Single<SimulationModel>(id);
            }
        }

        public long Add(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(simulation);
            }
        }

        public int Update(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(simulation);
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Delete<SimulationModel>(id);
            }
        }

        public string GetActualStatus(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                // Is it running right now?
                if (Loader.Instance.CurrentSimulation != null && simulation.Id == Loader.Instance.CurrentSimulation.Id)
                {
                    return "Running";
                }

                // Does simulation exist in database?
                if(!db.Exists<SimulationModel>(simulation.Id))
                {
                    return "Invalid";
                } 

                // Does cluster exist in database?
                if (!db.Exists<ClusterModel>(simulation.Cluster))
                {
                    return "Invalid";
                }

                // Do all required maps exist and valid
                var sql = Sql.Builder.Select("COUNT(*)").From("maps").Where("id = @0", simulation.Map).Where("status = @0", "Valid");
                int count = db.Single<int>(sql);
                if (count != 1)
                {
                    return "Invalid";
                }

                // Do all required vehicles exist and valid
                sql = Sql.Builder.Select("COUNT(*)").From("vehicles").Where("id IN (@0)", simulation.Vehicles).Where("status = @0", "Valid");
                count = db.Single<int>(sql);
                if (count != simulation.Vehicles.Split(',').Length)
                {
                    return "Invalid";
                }
            }

            return "Valid";
        }

        // TODO: these probably should be in different service
        public SimulationModel GetCurrent() => Loader.Instance.CurrentSimulation;
        public void Start(SimulationModel simulation) => Loader.StartAsync(simulation);
        public void Stop() => Loader.StopAsync();
    }
}
