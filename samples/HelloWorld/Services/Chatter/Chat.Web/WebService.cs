// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chat.Web
{
    using System.Collections.Generic;
    using Microsoft.ServiceFabric.Services;

    internal class WebService : StatelessService
    {
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new List<ServiceInstanceListener>() {
                new ServiceInstanceListener(
                    (initParams) =>
                       new OwinCommunicationListener("chatter", new Startup(), this.ServiceInitializationParameters))
                       };
        }

    }
}