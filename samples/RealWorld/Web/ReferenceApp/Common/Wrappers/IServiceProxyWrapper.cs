// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Common.Wrappers
{
    using System;

    /// <summary>
    /// Interface to wrap the static ServiceProxy methods so we can inject any implementation into components that
    /// need ServiceProxy. This way we can inject a mock for unit testing.
    /// </summary>
    public interface IServiceProxyWrapper
    {
        TServiceInterface Create<TServiceInterface>(Uri serviceName);
        TServiceInterface Create<TServiceInterface>(string partitionKey, Uri serviceName);
        TServiceInterface Create<TServiceInterface>(long partitionKey, Uri serviceName);
    }
}