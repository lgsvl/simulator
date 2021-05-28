/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using Simulator.Web;
using System.Collections.Generic;
using System.Linq;

namespace Simulator.Database.Services
{
    public class SimulationService : ISimulationService
    {
        public SimulationData Get(string simid)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("simid = @0", simid);
                Simulation sim = db.SingleOrDefault<Simulation>(sql);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(sim.simData);
            }
        }

        public void Add(SimulationData data)
        {
            using (var db = DatabaseManager.Open())
            {
                db.Insert(data);
            }
        }

        public void AddOrUpdate(SimulationData data)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("simid = @0", data.Id);
                Simulation sim = db.SingleOrDefault<Simulation>(sql);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, JsonSettings.camelCase);
                if (sim != null)
                {
                    sim.simData = json;
                    db.Update(sim);
                }
                else
                {
                    sim = new Simulation();
                    sim.simid = data.Id;
                    sim.simData = json;
                    db.Insert(sim);
                }
            }
        }

        public int Delete(SimulationData data)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("simid = @0", data.Id);
                Simulation sim = db.SingleOrDefault<Simulation>(sql);
                int result = db.Delete<Simulation>(sql);
                return result;
            }
        }

        public IEnumerable<SimulationData> List()
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder;
                IEnumerable<Simulation> simulations = db.Query<Simulation>(sql);
                return simulations.Select(s => Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(s.simData));
            }
        }
    }
}
