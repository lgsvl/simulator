/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IClusterService
    {
        IEnumerable<ClusterModel> List(int page, int count);
        ClusterModel Get(long id);
        long Add(ClusterModel cluster);
        int Update(ClusterModel cluster);
        int Delete(long id);
    }
}
