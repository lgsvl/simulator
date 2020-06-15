/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public interface IClientSettingsService
    {
        ClientSettings GetOrMake();
        void UpdateOnlineStatus(bool online);
    }
}
