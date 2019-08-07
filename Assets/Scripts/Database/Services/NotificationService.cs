/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Web;

namespace Simulator.Database.Services
{
    public class NotificationService : INotificationService
    {
        public void Send(string @event, object obj, string username)
        {
            NotificationManager.SendNotification(@event, obj, username);
        }
    }
}
