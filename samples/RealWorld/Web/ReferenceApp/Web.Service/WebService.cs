// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Web.Service
{
    using Microsoft.ServiceFabric.Services;

    internal class WebService : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new OwinCommunicationListener("fabrikam", new Startup()); //We can rename to whatever 
        }
    }
}