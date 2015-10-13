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

        async Task IBuildingSensorActor.ReceiveDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody)
        {
            await Task.Run(() =>
            {
                this.State.LatestMessageTime = timeOfEvent;
                this.State.LatestMessageProperties = JsonConvert.DeserializeObject<SensorMessage>(Encoding.UTF8.GetString(messageBody));
            });

            return;
        }        

        async Task<string> IBuildingSensorActor.GetBuildingIdAsync()
        {
            return await Task.FromResult(this.State.LatestMessageProperties.BuildingId);
        }

        async Task<string> IBuildingSensorActor.GetDeviceIdAsync()
        {
            return await Task.FromResult(this.State.LatestMessageProperties.DeviceId);
        }

        async Task<double> IBuildingSensorActor.GetHumityPercentageAsync()
        {
            return await Task.FromResult(this.State.LatestMessageProperties.Humidity);
        }

        async Task<bool> IBuildingSensorActor.GetLightStatusAsync()
        {
            return await Task.FromResult(this.State.LatestMessageProperties.Light);
        }

        async Task<double> IBuildingSensorActor.GetTemperatureInFahrenheitAsync()
        {
            return await Task.FromResult(this.State.LatestMessageProperties.TempF);
        }

        async Task<DateTime> IBuildingSensorActor.GetLastMessageTimeAsync()
        {
            return await Task.FromResult(this.State.LatestMessageTime);
        }

    }
}
