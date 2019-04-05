using System;
using Nancy;

public class MainMenuModule : NancyModule
{
    public MainMenuModule()
    {
        Get("/", x => {
            return View["mapSelect.html", BundleManager.instance.bundles.ToArray()];
        });

        Post("/", x => {
            string selection = Request.Form["mapSelect"];
            BundleManager.instance.Load(selection);
            return View["mapSelect.html", BundleManager.instance.bundles.ToArray()];
        });
    }
}
