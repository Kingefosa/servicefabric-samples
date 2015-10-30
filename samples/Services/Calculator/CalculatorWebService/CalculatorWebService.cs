// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace CalculatorWebService
{
    using Microsoft.ServiceFabric.Services;
    using System.Collections.Generic;

    public class CalculatorWebService : StatelessService
    {
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(initParams => new OwinCommunicationListener("calculator", new Startup(), initParams))
            };
        }
    }
}