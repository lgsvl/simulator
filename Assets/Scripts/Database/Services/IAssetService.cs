/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IAssetService
    {
        AssetModel Get(string guid);
        AssetModel Get(long id);
        long Add(AssetModel asset);
        int Update(AssetModel asset);
        int Delete(long id);
    }
}
