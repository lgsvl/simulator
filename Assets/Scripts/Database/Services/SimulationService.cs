/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using Simulator.Web;

namespace Simulator.Database.Services
{
    public class SimulationService : ISimulationService
    {
        public SimulationData Get(string simid)
        {
            using (var db = DatabaseManager.Open())
            {

            }
                return null;
        }

        public void Add(SimulationData data)
        {

        }
    }
}
