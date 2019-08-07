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
    public class SessionService : ISessionService
    {
        public IEnumerable<SessionModel> List()
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<SessionModel>(0, 100).Items;
            }
        }

        public bool Exists(string identity)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Exists<SessionModel>("WHERE cookie = @0", identity);
            }
        }

        public void Add(SessionModel model)
        {
            using (var db = DatabaseManager.Open())
            {
                db.Insert(model);
            }
        }

        public void Remove(string identity)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("cookie = @0", identity);
                db.Delete<SessionModel>(sql);
            }
        }

        public SessionModel Get(string identity)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("cookie = @0", identity);
                return db.SingleOrDefault<SessionModel>(sql);
            }
        }
    }
}
