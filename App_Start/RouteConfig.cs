using System.Web.Mvc;
using System.Web.Routing;

namespace CustomerPortal_MVC_
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            // Ignore requests for physical resource files (e.g., .axd)
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Static asset fallbacks for WebManifest & Favicons
            routes.MapRoute("WebManifestRoute", "site.webmanifest", new { controller = "Home", action = "WebManifest" });
            routes.MapRoute("Favicon32Root", "favicon-32x32.png", new { controller = "Home", action = "Favicon32" });
            routes.MapRoute("Favicon16Root", "favicon-16x16.png", new { controller = "Home", action = "Favicon16" });
            routes.MapRoute("Favicon32Icon", "Content/icon/favicon-32x32.png", new { controller = "Home", action = "Favicon32" });
            routes.MapRoute("Favicon16Icon", "Content/icon/favicon-16x16.png", new { controller = "Home", action = "Favicon16" });

            // Standard MVC Default Route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );
        }
    }
}