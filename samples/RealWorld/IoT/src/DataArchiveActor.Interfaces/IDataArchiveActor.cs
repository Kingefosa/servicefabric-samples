using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace DataArchiveActor.Interfaces
{
    public interface IDataArchiveActor : IActor
    {
        Task SaveDeviceStateAsync(SensorMessage message);
    }
}
