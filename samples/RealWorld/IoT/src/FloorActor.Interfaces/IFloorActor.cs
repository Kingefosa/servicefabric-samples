using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FloorActor.Interfaces
{
    public interface IFloorActor : IActor
    {
        Task<int> GetCountAsync();

        Task SetCountAsync(int count);
    }
}
