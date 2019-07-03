/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Nancy;
using System.IO;

namespace Simulator.Web.Modules
{
    public class IndexModule : NancyModule
    {
        static readonly string[] StaticAssets = new[]
        {
            "favicon.png",
            "main.css",
            "main.js",
        };
        
        public IndexModule()
        {
            Get("/", _ => ServeStaticAsset("/index.html"));

            foreach (var asset in StaticAssets)
            {
                Get($"/{asset}", _ => ServeStaticAsset(Request.Path));
            }
        }

        object ServeStaticAsset(string path)
        {
            // TODO: ideally WebUI should be built inside ApplicationRoot + "/Web" folder directly
            var file = Path.Combine(Config.Root, "WebUI", "dist", path.Substring(1));
            if (File.Exists(file))
            {
                return Response.AsFile(file);
            }

            file = Path.Combine(Config.Root, "Web", path.Substring(1));
            if (File.Exists(file))
            {
                return Response.AsFile(file);
            }

            return HttpStatusCode.NotFound;
        }
    }
}
