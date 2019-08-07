/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface ISessionService
    {
        IEnumerable<SessionModel> List();
        bool Exists(string identity);
        void Add(SessionModel model);
        void Remove(string identity);
        SessionModel Get(string identity);
    }
}
