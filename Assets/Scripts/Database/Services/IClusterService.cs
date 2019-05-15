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
        IEnumerable<Cluster> List(int page, int count);
        Cluster Get(long id);
        long Add(Cluster cluster);
        int Update(Cluster cluster);
        int Delete(long id);
    }
}
