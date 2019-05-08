/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IMapService : IDatabaseService
    {
        IEnumerable<Map> List(int page, int count);
        Map Get(long id);
        long Add(Map map);
        int Update(Map map);
        int Delete(long id);
    }
}
