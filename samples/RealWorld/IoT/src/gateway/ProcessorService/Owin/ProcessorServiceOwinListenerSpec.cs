using IoTProcessorManagement.Common;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace EventHubProcessor
{
    class ProcessorServiceOwinListenerSpec : IOwinListenerSpec
    {
        public IoTEventHubProcessorService Svc { get; set; }




        public void CreateOwinPipeline(IAppBuilder app)
        {
            //TODO: Map ADAL
            HttpConfiguration config = new HttpConfiguration();

            // inject state manager in relevant controllers 
            config.DependencyResolver = new ServiceRefInjector(this.Svc);
            // map API routes
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                            name: "DefaultApi",
                            routeTemplate: "{controller}/{id}",
                            defaults: new { id = RouteParameter.Optional }
            );

            // use the Web API
            app.UseWebApi(config);
        }
    }
}
