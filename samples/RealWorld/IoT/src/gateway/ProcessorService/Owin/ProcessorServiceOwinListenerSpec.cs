﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace EventHubProcessor
{
    using System.Web.Http;
    using IoTProcessorManagement.Common;
    using Owin;

    internal class ProcessorServiceOwinListenerSpec : IOwinListenerSpec
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
                defaults: new {id = RouteParameter.Optional}
                );

            // use the Web API
            app.UseWebApi(config);
        }
    }
}