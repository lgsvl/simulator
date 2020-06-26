/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Web;

namespace Simulator.Database.Services
{
    public interface ISimulationService
    {
        SimulationData Get(string simid);
        void Add(SimulationData data);
        void AddOrUpdate(SimulationData data);
    }
}
