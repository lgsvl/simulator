/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
*/

using PetaPoco;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Database.Services
{
    public class UserService : IUserService
    {
        public IEnumerable<UserModel> List(int page, int count)
        {
            using (var db = DatabaseManager.Open())
            {
                return db.Page<UserModel>(page, count).Items;
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
                var sql = Sql.Builder.Select("COUNT(*)").From("users").Where("username = @0", user.Username);
                if (db.Single<int>(sql) > 0)
                {
                    // WRONG: Update returns number of updated rows
                    return Update(user);
                }
                else
                {
                    // WRONG: Add returns primary key
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
