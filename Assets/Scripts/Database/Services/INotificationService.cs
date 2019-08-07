/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Database.Services
{
    public interface INotificationService
    {
        void Send(string @event, object obj, string username);
    }
}
