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
    public class UserService : IUserService
    {
        public IEnumerable<UserModel> List(string filter, int offset, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    var cleanFilter = $"%{filter.Replace("%", "").Replace("_", "")}%";
                    var sql = Sql.Builder
                        .Where(@"
                            (username LIKE @0) OR
                            (firstName LIKE @0) OR
                            (lastName LIKE @0) OR
                            (organization LIKE @0) OR
                            (role LIKE @0)", cleanFilter)
                        .OrderBy("id")
                        .Append("LIMIT @0, @1", offset, count);
                    return db.Fetch<UserModel>(sql);

                }
                return db.Fetch<UserModel>("LIMIT @0, @1", offset, count);
            }
        }

        public UserModel Get(string username)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("username = @0", username);
                return db.SingleOrDefault<UserModel>(sql);
            }
        }

        public long Add(UserModel user)
        {
            using (var db = DatabaseManager.Open())
            {
                return (long)db.Insert("users", "Id", true, user);
            }
        }

        public int Update(UserModel user)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Update(user);
            }
        }

        public long AddOrUpdate(UserModel user)
        {
            using (var db = DatabaseManager.Open())
            {
                var sql = Sql.Builder.Where("username = @0", user.Username);
                var old = db.SingleOrDefault<UserModel>(sql);
                if (old != null)
                {
                    user.Id = old.Id;
                    Update(user);
                    return user.Id;
                }
                else
                {
                    return Add(user);
                }
            }
        }

        public int Delete(long id)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Delete<UserModel>(id);
            }
        }
    }
}
