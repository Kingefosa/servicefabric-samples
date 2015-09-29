// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Common.Wrappers
{
    using System;
    using Microsoft.ServiceFabric.Services;

    /// <summary>
    /// Wrapper class for the static ServiceProxy.
    /// </summary>
    public class ServiceProxyWrapper : IServiceProxyWrapper
    {
        public TServiceInterface Create<TServiceInterface>(Uri serviceName)
        {
            return ServiceProxy.Create<TServiceInterface>(serviceName);
        }

        public TServiceInterface Create<TServiceInterface>(long partitionKey, Uri serviceName)
        {
            return ServiceProxy.Create<TServiceInterface>(partitionKey, serviceName);
        }

        public TServiceInterface Create<TServiceInterface>(string partitionKey, Uri serviceName)
        {
            return ServiceProxy.Create<TServiceInterface>(partitionKey, serviceName);
        }
    }
}