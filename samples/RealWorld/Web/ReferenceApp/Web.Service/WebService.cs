// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Web.Service
{
    using Microsoft.ServiceFabric.Services;

    internal class WebService : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new OwinCommunicationListener("fabrikam", new Startup());
        }
    }
}