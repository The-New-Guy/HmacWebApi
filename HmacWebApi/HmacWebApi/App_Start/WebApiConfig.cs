using NGY.API.Authentication;
using NGY.API.Authentication.HMAC;
using System.Web.Http;

namespace HmacWebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Hook up authentication message handlers.
            //config.MessageHandlers.Add(new HmacAuthenticationHandler(new DummySecretRepository(), new HmacCanonicalRepresentationBuilder(), new HmacSignatureCalculator()));
            //config.MessageHandlers.Add(new ResponseContentMd5Handler());

            config.MessageHandlers.Add(new CacheHandler(new DummySecretRepository(), new HmacCanonicalRepresentationBuilder(), new HmacSignatureCalculator()));
            config.MessageHandlers.Add(new ResponseContentMd5Handler());

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
        }
    }
}
