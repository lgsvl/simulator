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
        IEnumerable<ClusterModel> List(string filter, int offset, int count, string owner);
        bool Validate(long id, string owner);
        ClusterModel Get(long id, string owner);
        long Add(ClusterModel cluster);
        int Update(ClusterModel cluster);
        int Delete(long id, string owner);
    }
}
