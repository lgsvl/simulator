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
        IEnumerable<VehicleModel> List(string filter, int offset, int count, string owner);
        VehicleModel Get(string guid);
        VehicleModel Get(long id);
        long Add(VehicleModel vehicle);
        int Update(VehicleModel vehicle);
        void SetStatusForPath(string status, string localPath);
        int GetCountOfLocal(string localPath);
        int GetCountOfUrl(string url);
        List<VehicleModel> GetAllMatchingUrl(string url);
        int Delete(long id);
    }
}
