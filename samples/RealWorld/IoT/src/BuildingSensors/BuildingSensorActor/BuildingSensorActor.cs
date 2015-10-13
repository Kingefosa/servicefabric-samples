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
    public class BuildingSensorActor : Actor<BuildingSensorActorState>, IBuildingSensorActor
    {
        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new BuildingSensorActorState() { LatestMessageProperties = new SensorMessage() };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }

        Task IBuildingSensorActor.ReceiveDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody)
        {
            return Task.Run(() =>
            {
                this.State.LatestMessageTime = timeOfEvent;
                this.State.LatestMessageProperties = JsonConvert.DeserializeObject<SensorMessage>(Encoding.UTF8.GetString(messageBody));
            });
        }

        Task<SensorMessage> IBuildingSensorActor.GetLastMessageAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties);
        }

        Task<string> IBuildingSensorActor.GetBuildingIdAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.BuildingId);
        }

        Task<string> IBuildingSensorActor.GetDeviceIdAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.DeviceId);
        }

        Task<double> IBuildingSensorActor.GetHumityPercentageAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Humidity);
        }

        Task<bool> IBuildingSensorActor.GetLightStatusAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Light);
        }

        Task<double> IBuildingSensorActor.GetTemperatureInFahrenheitAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.TempF);
        }

        Task<DateTime> IBuildingSensorActor.GetLastMessageTimeAsync()
        {
            return Task.FromResult(this.State.LatestMessageTime);
        }
    }
}
