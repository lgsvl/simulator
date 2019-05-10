/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IVehicleService : IService
    {
        IEnumerable<Vehicle> List(int page, int count);
        Vehicle Get(long id);
        long Add(Vehicle vehicle);
        int Update(Vehicle vehicle);
        int Delete(long id);
    }
}
