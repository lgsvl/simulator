/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IUserService
    {
        IEnumerable<UserModel> List(int page, int count);
        UserModel Get(string username);
        long Add(UserModel user);
        int Update(UserModel user);
        long AddOrUpdate(UserModel user);
        int Delete(long id);
    }
}
