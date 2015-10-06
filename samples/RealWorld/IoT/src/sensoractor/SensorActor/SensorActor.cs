using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SensorActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace SensorActor
{
    public class SensorActor : Actor<SensorActorState>, ISensorActor
    {
        public Task ReceiveDeviceState(DateTime timeOfEvent, byte[] messageBody)
        {
            this.State.LatestEventTime = timeOfEvent;

            throw new NotImplementedException();

            //todo: need to figure out message format.  Just JSON I have to parse?
        }

        public Task<T> GetProperty<T>(string propertyName)
        {
            object value = null;
            this.State.LatestEventProperties.TryGetValue(propertyName, out value);
            return Task.FromResult<T>((T)value);
        }

        public Task<List<T>> GetPropertyHistory<T>(string propertyName)
        {
      
            List<ISensorDetails> propertyHistory =
                this.State.Properties.Where(details => details.PropertyName == propertyName).OrderBy(details => details.Time).ToList();

            List<T> values = new List<T>();
            propertyHistory.ForEach(details => values.Add((T)details.Value));

            return Task.FromResult(values);

        }

        public async Task<double> GetPropertyAverageAsync<T>(string propertyName)
        {
            List<T> values = await GetPropertyHistory<T>(propertyName);

            double average = values.Average<T>(x => Double.Parse(x.ToString()));
            return average;
        }

        public Task<TimeSpan> GetHistoryTimeSpanAsync()
        {
            List<ISensorDetails> propertyHistory =
                this.State.Properties.OrderBy(details => details.Time).ToList();

            TimeSpan propertyHistoryTimeSpan = propertyHistory.Last().Time - propertyHistory.First().Time;

            return Task.FromResult(propertyHistoryTimeSpan);
        }

      
 
    }
}
