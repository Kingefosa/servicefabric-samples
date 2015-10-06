using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace SensorActor.Interfaces
{
    public interface ISensorActor : IActor
    {
         Task ReceiveDeviceState(DateTime timeOfEvent, byte[] messageBody);

        Task<T> GetProperty<T >(string propertyName);

        Task<List<T>> GetPropertyHistory<T>(string propertyName);

        Task<double> GetPropertyAverageAsync<T>(string propertyName);

        Task<TimeSpan> GetHistoryTimeSpanAsync();
    }
}
