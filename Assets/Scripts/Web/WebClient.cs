using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Web
{
    public class WebClient
    {
        public static HashSet<WebClient> Clients = new HashSet<WebClient>();

        public ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        public SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);

        public static void SendNotification(string data)
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    client.Queue.Enqueue(data);
                    client.Semaphore.Release();
                }
            }
        }
    }
}
