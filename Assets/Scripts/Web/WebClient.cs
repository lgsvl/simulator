using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Web
{
    public class WebClient
    {
        public static HashSet<WebClient> Clients = new HashSet<WebClient>();

        public ConcurrentQueue<ClientMessage> Queue = new ConcurrentQueue<ClientMessage>();
        public SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);

        public static void SendNotification(ClientMessage message)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    client.Queue.Enqueue(message);
                    client.Semaphore.Release();
                }
            }
        }
    }

    public class ClientMessage
    {
        public string eventName;
        public string data;

        public ClientMessage(string _eventName, string _data)
        {
            eventName = _eventName;
            data = _data;
        }
    }
}
