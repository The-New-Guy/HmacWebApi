using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace HmacWebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Enable tracining.
            config.EnableSystemDiagnosticsTracing();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "OutOfBounds",
                routeTemplate: "{*pathInfo}",
                defaults: new { controller = "Default", pathInfo = RouteParameter.Optional }
            );
        }
    }
}
