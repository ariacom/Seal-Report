using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SealWebServer
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            /*routes.MapRoute(
                name: "Default",
                url: "{action}/{id}",
                defaults: new { controller = "Home", action = "Main", id = UrlParameter.Optional }
            );
            */
            
            routes.MapRoute(
                 "Default", // Route name
                 "{action}", // URL with parameters
                 new { controller = "Home", action = "Main" } // Parameter defaults
                 );

        }
    }
}