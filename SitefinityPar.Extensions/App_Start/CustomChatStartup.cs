using System.Web.Http;
using System.Web.Routing;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(SitefinityWebApp.App_Start.CustomChatStartup), "Start")]

namespace SitefinityWebApp.App_Start
{
    public static class CustomChatStartup
    {
        public static void Start()
        {
            var customRoutes = new RouteCollection();
            customRoutes.MapHttpRoute(
                name: "AgenticRagCustomChat",
                routeTemplate: "api/default/AgenticRag/Chat",
                defaults: new { controller = "CustomChat", action = "CustomChat" },
                constraints: new { httpMethod = new HttpMethodConstraint("POST") });
            customRoutes.MapHttpRoute(
                name: "CustomChat",
                routeTemplate: "customchat/chat",
                defaults: new { controller = "CustomChat", action = "CustomChat" });

            // Give the hosted Web API routes priority over Sitefinity's catch-all routes.
            var routes = RouteTable.Routes;
            using (routes.GetWriteLock())
            {
                for (var index = customRoutes.Count - 1; index >= 0; index--)
                {
                    routes.Insert(0, customRoutes[index]);
                }
            }
        }
    }
}
