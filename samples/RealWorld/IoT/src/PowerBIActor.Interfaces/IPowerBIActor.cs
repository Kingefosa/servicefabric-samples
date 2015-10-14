using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace PowerBIActor.Interfaces
{
    public interface IPowerBIActor : IActor
    {
        Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body);
    }
}
