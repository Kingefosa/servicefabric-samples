using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace BuildingSensorActor.Interfaces
{
    public interface IBuildingSensorActor : IActor
    {
        Task ReceiveDeviceState(DateTime timeOfEvent, byte[] messageBody);

        Task<DateTime> GetLastMessageTime();
        Task<string> GetDeviceId();
        Task<string> GetBuildingId();
        Task<double> GetTemperatureInFahrenheit();
        Task<double> GetHumityPercentage();
        Task<bool> GetLightStatus();
    }
}
