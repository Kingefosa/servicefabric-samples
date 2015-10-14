using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloorActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace FloorActor
{
    public class FloorActor : Actor<FloorActorState>, IFloorActor
    {
        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new FloorActorState() { Count = 0 };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }
        public Task SendDeviceStateAsync(SensorMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
