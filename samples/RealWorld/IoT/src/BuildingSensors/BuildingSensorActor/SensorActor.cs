using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingSensorActor.Interfaces;
using BuildingSensorActor.Common;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using System.Text;

namespace BuildingSensorActor
{
    public class SensorActor : Actor<SensorActorState>, ISensorActor
    {
        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new SensorActorState() { LatestMessageProperties = new SensorMessage() };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }

        Task ISensorActor.ReceiveDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody)
        {
            return Task.Run(() =>
            {
                this.State.LatestMessageTime = timeOfEvent;
                this.State.LatestMessageProperties = JsonConvert.DeserializeObject<SensorMessage>(Encoding.UTF8.GetString(messageBody));
            });
        }

        Task<SensorMessage> ISensorActor.GetLastMessageAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties);
        }

        Task<string> ISensorActor.GetBuildingIdAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.BuildingId);
        }

        Task<string> ISensorActor.GetDeviceIdAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.DeviceId);
        }

        Task<double> ISensorActor.GetHumityPercentageAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Humidity);
        }

        Task<bool> ISensorActor.GetLightStatusAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Light);
        }

        Task<double> ISensorActor.GetTemperatureInFahrenheitAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.TempF);
        }

        Task<DateTime> ISensorActor.GetLastMessageTimeAsync()
        {
            return Task.FromResult(this.State.LatestMessageTime);
        }
    }
}
