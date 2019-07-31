/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System.Linq;
using System.Collections.Generic;
using Nancy.Extensions;
using Simulator.Web;

namespace Simulator.Database.Services
{
    public class SimulationService : ISimulationService
    {
        public IEnumerable<SimulationModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    List<SimulationModel> simulations = db.Page<SimulationModel>(page, count).Items;
                    foreach (var sim in simulations)
                    {
                        sim.Vehicles = db.Query<ConnectionModel>(Sql.Builder.Where("simulation = @0", sim.Id)).ToArray();
                    }
                    db.CompleteTransaction();
                    return simulations;
                }
                catch
                {
                    db.AbortTransaction();
                    throw;
                }
            }
        }

        public SimulationModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    var sim = db.Single<SimulationModel>(id);
                    sim.Vehicles = db.Query<ConnectionModel>(Sql.Builder.Where("simulation = @0", sim.Id)).ToArray();
                    db.CompleteTransaction();
                    return sim;
                }
                catch
                {
                    db.AbortTransaction();
                    throw;
                }
            }
        }

        public long Add(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    ClearConnections(db, simulation.Id);
                    simulation.Id = (long)db.Insert(simulation);
                    UpdateConnections(db, simulation);
                    db.CompleteTransaction();
                    return simulation.Id;
                }
                catch
                {
                    db.AbortTransaction();
                    throw;
                }
            }
        }

        public int Update(SimulationModel simulation)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    ClearConnections(db, simulation.Id);

                    UpdateConnections(db, simulation);
                    int result = db.Update(simulation);
                    db.CompleteTransaction();
                    return result;
                }
                catch
                {
                    db.AbortTransaction();
                    throw;
                }
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    ClearConnections(db, id);
                    int result = db.Delete<SimulationModel>(id);
                    db.CompleteTransaction();
                    return result;
                }
                catch
                {
                    db.AbortTransaction();
                    throw;
                }
            }
        }

        private void ClearConnections(IDatabase db, long id)
        {
            db.Delete<ConnectionModel>(Sql.Builder.Where("simulation = @0", id));
        }

        private void UpdateConnections(IDatabase db, SimulationModel simulation)
        {
            foreach(var connection in simulation.Vehicles)
            {
                connection.Simulation = simulation.Id;
                db.Insert(connection);
            }
        }

        public string GetActualStatus(SimulationModel simulation, bool allowDownloading)
        {
            using (var db = DatabaseManager.Open())
            {
                // Is it running right now?
                if (Loader.Instance.CurrentSimulation != null && simulation.Id == Loader.Instance.CurrentSimulation.Id)
                {
                    return "Running";
                }

                // Does cluster exist in database?
                if (!db.Exists<ClusterModel>(simulation.Cluster))
                {
                    return "Invalid";
                }

                if (simulation.ApiOnly.GetValueOrDefault())
                {
                    return "Valid";
                }

                // Does maps exists and is valid
                MapModel map = db.SingleOrDefault<MapModel>(simulation.Map);
                if (map == null)
                {
                    return "Invalid";
                }

                if (allowDownloading && map.Status == "Downloading")
                {
                    // allow it
                }
                else if (map.Status != "Valid")
                {
                    return "Invalid";
                }

                if (simulation.Vehicles == null || simulation.Vehicles.Length == 0)
                {
                    return "Invalid";
                }

                // Do all required vehicles exist and are valid
                var sql = Sql.Builder.Select("COUNT(*)").From("vehicles").Where("id IN (@0)", simulation.Vehicles.Select(v => v.Vehicle).ToArray());
                var count = allowDownloading ? db.Single<int>(sql.Where("status != @0", "Invalid")) : db.Single<int>(sql.Where("status = @0", "Valid"));

                if (simulation.Vehicles.DistinctBy(v => v.Vehicle).Count() != count)
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
