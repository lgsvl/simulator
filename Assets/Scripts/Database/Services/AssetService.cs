/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public class AssetService : IAssetService
    {
        public AssetModel Get(string guid)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("guid = @0", guid);
                return db.SingleOrDefault<AssetModel>(sql);
            }
        }

        public AssetModel Get(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id);
                return db.Single<AssetModel>(sql);
            }
        }

        public long Add(AssetModel asset)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert(asset);
            }
        }

        public int Update(AssetModel asset)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(asset);
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("id = @0", id);
                return db.Delete<AssetModel>(sql);
            }
        }
    }
}
