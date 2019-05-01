using Nancy.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Web
{
    public class NotificationManager
    {
        public static HashSet<NotificationManager> Clients = new HashSet<NotificationManager>();

        public BlockingCollection<ClientMessage> Queue = new BlockingCollection<ClientMessage>();

        public static JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static void SendNotification(ClientMessage message)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    client.Queue.Add(message);
                }
            }
        }
    }

    public class ClientMessage
    {
        public string eventName;
        public string data;

        public ClientMessage(string _eventName, object _data)
        {
            eventName = _eventName;
            data = NotificationManager.Serializer.Serialize(_data);
        }
    }
}
