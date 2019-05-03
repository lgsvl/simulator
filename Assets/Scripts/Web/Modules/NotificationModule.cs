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
            Get("/events", _ =>
            {
                var r = new Response();
                r.Headers.Add("Cache-Control", "no-cache");
                r.ContentType = "text/event-stream";
                r.Contents = (Func<Stream, Task>)(async stream =>
                {
                    var client = new NotificationManager();
                    lock (NotificationManager.Clients)
                    {
                        NotificationManager.Clients.Add(client);
                    }

                    try
                    {
                        using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                        {
                            await writer.FlushAsync();

                            while (true)
                            {
                                var message = await Task.Run(() => client.Queue.Take());
                                await writer.WriteAsync("event: ");
                                await writer.WriteLineAsync(message.eventName);
                                await writer.WriteAsync("data: ");
                                await writer.WriteLineAsync(message.data);
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                            }
                        }
                    }
                    finally
                    {
                        lock (NotificationManager.Clients)
                        {
                            NotificationManager.Clients.Remove(client);
                        }
                    }
                });
                return r;
            });
        }
    }
}
