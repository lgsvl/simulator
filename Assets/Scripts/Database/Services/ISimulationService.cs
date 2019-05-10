/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface ISimulationService : IService
    {
        IEnumerable<Simulation> List(int page, int count);
        Simulation Get(long id);
        long Add(Simulation simulation);
        int Update(Simulation simulation);
        int Delete(long id);

        string GetActualStatus(Simulation simulation);
        Simulation GetCurrent();
        void Start(Simulation simulation);
        void Stop();
    }
}
