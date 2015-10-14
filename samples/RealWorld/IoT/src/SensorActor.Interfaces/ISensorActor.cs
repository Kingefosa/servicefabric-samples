using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace SensorActor.Interfaces
{
    public interface ISensorActor : IActor
    {
        Task SendDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody);
        Task<SensorMessage> GetLastMessageAsync();
        Task<DateTime> GetLastMessageTimeAsync();
        Task<string> GetDeviceIdAsync();
        Task<string> GetBuildingIdAsync();
        Task<double> GetTemperatureInFahrenheitAsync();
        Task<double> GetHumityPercentageAsync();
        Task<bool> GetLightStatusAsync();
    }
}
