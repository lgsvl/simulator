using System.IO;

using Nancy;

namespace Web.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get("/", _ => Response.AsFile(
                Path.Combine(MainMenu.ApplicationRoot, "views", "index.html"))
            );
        }
    }
}