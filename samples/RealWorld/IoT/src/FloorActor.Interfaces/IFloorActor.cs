using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace FloorActor.Interfaces
{
    public interface IFloorActor : IActor
    {
        Task ReceiveMessageAsync(SensorMessage m);
        Task<string> GetFloorIdAsync();
        Task<double> GetAverageTemperatureForLastMinuteAsync();
        Task<double> GetAverageHumidityForLastMinuteAsync();

    }
}
