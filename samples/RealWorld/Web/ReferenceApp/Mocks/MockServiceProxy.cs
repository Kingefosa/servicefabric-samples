// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Mocks
{
    using System;
    using System.Collections.Generic;
    using Common;
    using Common.Wrappers;

    public class MockServiceProxy : IServiceProxyWrapper
    {
        private IDictionary<Type, Func<Uri, object>> createFunctions = new Dictionary<Type, Func<Uri, object>>();

        public TServiceInterface Create<TServiceInterface>(Uri serviceName)
        {
            return (TServiceInterface) this.createFunctions[typeof(TServiceInterface)](serviceName);
        }

        public TServiceInterface Create<TServiceInterface>(long partitionKey, Uri serviceName)
        {
            return (TServiceInterface) this.createFunctions[typeof(TServiceInterface)](serviceName);
        }

        public TServiceInterface Create<TServiceInterface>(string partitionKey, Uri serviceName)
        {
            return (TServiceInterface) this.createFunctions[typeof(TServiceInterface)](serviceName);
        }

        public void Supports<TServiceInterface>(Func<Uri, object> Create)
        {
            this.createFunctions[typeof(TServiceInterface)] = Create;
        }
    }
}