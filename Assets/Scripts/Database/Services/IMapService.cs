/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IMapService
    {
        IEnumerable<MapModel> List(string filter, int offset, int count, string owner);
        string GetExistingLocalPath(string url);
        int GetCountOfLocal(string localPath);
        MapModel Get(long id, string owner);
        long Add(MapModel map);
        int Update(MapModel map);
        int Delete(long id, string owner);
    }
}
