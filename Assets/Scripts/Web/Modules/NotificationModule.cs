using Nancy;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Web
{ 
    public class NotificationModule : NancyModule
    {
        public NotificationModule()
        {
            Get($"/notify", _ =>
            {
                var r = new Response();
                r.Headers.Add("Cache-Control", "no-cache");
                r.ContentType = "text/event-stream";
                r.Contents = (Func<Stream, Task>)(async stream =>
                {
                    var client = new WebClient();
                    lock (WebClient.Clients)
                    {
                        WebClient.Clients.Add(client);
                    }

                    try
                    {
                        using (var s = client.Semaphore)
                        using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                        {
                            while (true)
                            {
                                await client.Semaphore.WaitAsync();

                                string data;
                                while (client.Queue.TryDequeue(out data))
                                {
                                    await writer.WriteAsync("data: ");
                                    await writer.WriteLineAsync(data);
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                }
                            }
                        }
                    }
                    finally
                    {
                        lock (WebClient.Clients)
                        {
                            WebClient.Clients.Remove(client);
                        }
                    }
                });
                return r;
            });
        }
    }
}
