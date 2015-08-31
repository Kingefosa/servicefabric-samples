// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ClusterMonitor
{
    using System.Fabric.Description;
    using Microsoft.ServiceFabric.Services;

    public class Service : StatelessService
    {
        public const string ServiceTypeName = "ClusterMonitorType";

        protected override ICommunicationListener CreateCommunicationListener()
        {
            ConfigurationSettings configSettings =
                this.ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;

            return new OwinCommunicationListener("cluster", new Startup(configSettings));
        }
    }
}