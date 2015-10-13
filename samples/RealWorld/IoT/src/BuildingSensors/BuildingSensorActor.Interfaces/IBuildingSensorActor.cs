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
        Task ReceiveDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody);

        Task<DateTime> GetLastMessageTimeAsync();
        Task<string> GetDeviceIdAsync();
        Task<string> GetBuildingIdAsync();
        Task<double> GetTemperatureInFahrenheitAsync();
        Task<double> GetHumityPercentageAsync();
        Task<bool> GetLightStatusAsync();
    }
}
