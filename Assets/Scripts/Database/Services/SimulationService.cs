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

namespace Simulator.Database.Services
{
    public class SimulationService : ISimulationService
    {
        public IEnumerable<SimulationModel> List(string filter, int offset, int count, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var cleanFilter = $"%{filter.Replace("%", "").Replace("_", "")}%";
                        var filterSql = Sql.Builder
                            .Where(@"(name LIKE @0)", cleanFilter)
                            .OrderBy("id")
                            .Append("LIMIT @0, @1", offset, count);

                        List<SimulationModel> filteredSimulations = db.Fetch<SimulationModel>(filterSql);

                        foreach (var sim in filteredSimulations)
                        {
                            sim.Vehicles = db.Query<ConnectionModel>(Sql.Builder
                                .Where("simulation = @0", sim.Id)
                                .OrderBy("id")).ToArray();
                        }

                        db.CompleteTransaction();
                        return filteredSimulations;
                    }

                    var sql = Sql.Builder
                        .Where("owner = @0 OR owner IS NULL", owner)
                        .OrderBy("id")
                        .Append("LIMIT @0, @1", offset, count);

                    List<SimulationModel> simulations = db.Fetch<SimulationModel>(sql);

                    foreach (var sim in simulations)
                    {
                        sim.Vehicles = db.Query<ConnectionModel>(Sql.Builder.Where("simulation = @0", sim.Id).OrderBy("id")).ToArray();
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

        public SimulationModel Get(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                    var sim = db.Single<SimulationModel>(sql);
                    sim.Vehicles = db.Query<ConnectionModel>(Sql.Builder.Where("simulation = @0", sim.Id).OrderBy("id")).ToArray();
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

        public int Delete(long id, string owner)
        {
            using (var db = DatabaseManager.Open())
            {
                db.BeginTransaction();
                try
                {
                    ClearConnections(db, id);
                    var sql = Sql.Builder.Where("id = @0", id).Where("owner = @0 OR owner IS NULL", owner);
                    int result = db.Delete<SimulationModel>(sql);
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

        public void GetActualStatus(SimulationModel simulation, bool allowDownloading)
        {
            simulation.Error = "";

            using (var db = DatabaseManager.Open())
            {
                // Is it running right now?
                if (Loader.Instance.CurrentSimulation != null && simulation.Id == Loader.Instance.CurrentSimulation.Id)
                {
                    simulation.Status = "Running";
                    return;
                }

                // Does cluster exist in database?
                if (!db.Exists<ClusterModel>(simulation.Cluster))
                {
                    simulation.Error = "Cluster does not exist";
                    simulation.Status = "Invalid";
                    return;
                }

                if (simulation.ApiOnly.GetValueOrDefault())
                {
                    simulation.Status = "Valid";
                    return;
                }

                // Does maps exists and is valid
                MapModel map = db.SingleOrDefault<MapModel>(simulation.Map);
                if (map == null)
                {
                    simulation.Error = "Map does not exist";
                    simulation.Status = "Invalid";
                    return;
                }

                if (allowDownloading && map.Status == "Downloading")
                {
                    // allow it
                }
                else if (map.Status != "Valid")
                {
                    simulation.Error = map.Error;
                    simulation.Status = "Invalid";
                    return;
                }

                if (simulation.Vehicles == null || simulation.Vehicles.Length == 0)
                {
                    simulation.Error = "Vehicle does not exist";
                    simulation.Status = "Invalid";
                    return;
                }

                // Do all required vehicles exist and are valid
                var sql = Sql.Builder.Select("COUNT(*)").From("vehicles").Where("id IN (@0)", simulation.Vehicles.Select(v => v.Vehicle).ToArray());
                var count = allowDownloading ? db.Single<int>(sql.Where("status != @0", "Invalid")) : db.Single<int>(sql.Where("status = @0", "Valid"));

                if (simulation.Vehicles.DistinctBy(v => v.Vehicle).Count() != count)
                {
                    simulation.Error = "One of added vehicles is not valid";
                    simulation.Status = "Invalid";
                    return;
                }
            }

            simulation.Status = "Valid";
        }

        // TODO: these probably should be in different service
        public SimulationModel GetCurrent(string owner)
        {
            if (Loader.Instance.CurrentSimulation == null)
            {
                return null;
            }

            if (Loader.Instance.CurrentSimulation.Owner != null && Loader.Instance.CurrentSimulation.Owner != owner)
            {
                return null;
            }

            return Loader.Instance.CurrentSimulation;
        }

        public void Start(SimulationModel simulation)
        {
            Loader.StartAsync(simulation);
        }

        public void Stop()
        {
            Loader.StopAsync();
        }
    }
}
