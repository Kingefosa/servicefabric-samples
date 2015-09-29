// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace RestockRequestManager.Domain
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;

    public interface IRestockRequestManager : IService
    {
        Task AddRestockRequestAsync(RestockRequest request);
    }
}