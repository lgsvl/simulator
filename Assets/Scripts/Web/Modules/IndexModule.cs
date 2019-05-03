using System.IO;
using Nancy;

namespace Web.Modules
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
            var file = Path.Combine(MainMenu.ApplicationRoot, "WebUI", "dist", path.Substring(1));
            if (File.Exists(file))
            {
                return Response.AsFile(file);
            }

            file = Path.Combine(MainMenu.ApplicationRoot, "Web", path.Substring(1));
            if (File.Exists(file))
            {
                return Response.AsFile(file);
            }

            return HttpStatusCode.NotFound;
        }
    }
}
