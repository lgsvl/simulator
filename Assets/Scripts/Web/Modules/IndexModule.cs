using Nancy;
using System.IO;

namespace Simulator.Web.Modules
{
    public class IndexModule : NancyModule
    {
        static readonly string[] StaticAssets = new[]
        {
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
            var file = $"WebUI/dist/{path}";
            if (File.Exists(Path.Combine(Config.Root, file)))
            {
                return Response.AsFile(file);
            }

            file = $"Web/dist/{path}";
            if (File.Exists(Path.Combine(Config.Root, file)))
            {
                return Response.AsFile(file);
            }

            return HttpStatusCode.NotFound;
        }
    }
}
