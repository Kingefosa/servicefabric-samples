// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace RestockRequest.Domain
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    public interface IRestockRequestActor : IActor, IActorEventPublisher<IRestockRequestEvents>
    {
        Task AddRestockRequestAsync(RestockRequest request);
    }
}