/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IVehicleService
    {
        IEnumerable<VehicleModel> List(int page, int count);
        VehicleModel Get(long id);
        long Add(VehicleModel vehicle);
        int Update(VehicleModel vehicle);
        int GetCountOfLocal(string localPath);
        int Delete(long id);
    }
}
