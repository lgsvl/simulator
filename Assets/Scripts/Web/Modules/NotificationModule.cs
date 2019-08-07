/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Security;
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
            this.RequiresAuthentication();

            Get("/events", _ =>
            {
                var r = new Response();
                r.Headers.Add("Cache-Control", "no-cache");
                r.ContentType = "text/event-stream";
                r.Contents = (Func<Stream, Task>)(async stream =>
                {
                    var client = new NotificationManager(this.Context.CurrentUser.Identity.Name);
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
                                await writer.WriteLineAsync(message.Event);
                                await writer.WriteAsync("data: ");
                                await writer.WriteLineAsync(message.Data);
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
