/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using Nancy.Responses;
using Simulator.Database;
using System.IO;

namespace Simulator.Web.Modules
{
    public class DownloadModule : NancyModule
    {
        public DownloadModule() : base("download")
        {
#if ENABLE_FOR_CLUSTERS
            Get("/map/{name}", x =>
            {
                string name = x.name;

                // TODO: authenticate client
                // TODO: proper DB stuff

                using (var db = DatabaseManager.Open())
                {
                    var map = db.First<MapModel>(PetaPoco.Sql.Builder.Where("name = @0", name));

                    var file = new FileStream(map.LocalPath, FileMode.Open);
                    var response = new StreamResponse(() => file, "application/octet-stream");
                    return response.AsAttachment(name);
                }
            });

            Get("/vehicle/{name}", x =>
            {
                string name = x.name;

                // TODO: authenticate client
                // TODO: proper DB stuff

                using (var db = DatabaseManager.Open())
                {
                    var vehicle = db.First<VehicleModel>(PetaPoco.Sql.Builder.Where("name = @0", name));

                    var file = new FileStream(vehicle.LocalPath, FileMode.Open);
                    var response = new StreamResponse(() => file, "application/octet-stream");
                    return response.AsAttachment(name);
                }
            });
#endif
        }
    }
}
