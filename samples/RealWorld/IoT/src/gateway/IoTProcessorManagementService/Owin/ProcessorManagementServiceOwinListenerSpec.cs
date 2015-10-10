using IoTProcessorManagement.Common;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using System.Web.Http;

namespace IoTProcessorManagementService
{
    /// <summary>
    /// This class helps in building an Owin pipeline specific 
    /// to the need of the controller service. 
    /// CtrlSvc need to need to
    /// 1- Map Web API 
    /// 2- Inject state management in each controller created
    /// 3- TODO: Use ADAL to authenticate requests 
    /// </summary>
    class ProcessorManagementServiceOwinListenerSpec : IOwinListenerSpec
    {
        public ProcessorManagementService Svc { get; set; }
        



        public void CreateOwinPipeline(IAppBuilder app)
        {
            //TODO: Map ADAL
            HttpConfiguration config = new HttpConfiguration();

            // inject state manager in relevant controllers 
            config.DependencyResolver = new ProcessorManagementServiceRefInjector(this.Svc);
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
