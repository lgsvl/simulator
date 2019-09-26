/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System;
using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public class SessionService : ISessionService
    {
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
                var sql = Sql.Builder.Where("cookie = @0", identity).Where("expire > @0", DateTime.UtcNow);
                return db.SingleOrDefault<SessionModel>(sql);
            }
        }
    }
}
