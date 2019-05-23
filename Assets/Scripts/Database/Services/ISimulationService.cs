/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface ISimulationService
    {
        IEnumerable<SimulationModel> List(int page, int count);
        SimulationModel Get(long id);
        long Add(SimulationModel simulation);
        int Update(SimulationModel simulation);
        int Delete(long id);

        string GetActualStatus(SimulationModel simulation);
        SimulationModel GetCurrent();
        void Start(SimulationModel simulation);
        void Stop();
    }
}
