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

        Task IBuildingSensorActor.ReceiveDeviceState(DateTime timeOfEvent, byte[] messageBody)
        {
            return Task.Run(() =>
            {
                this.State.LatestMessageTime = timeOfEvent;

                this.State.LatestMessageProperties = JsonConvert.DeserializeObject<SensorMessage>(Encoding.UTF8.GetString(messageBody));
            });
        }        

        Task<string> IBuildingSensorActor.GetBuildingId()
        {
            return Task.FromResult(this.State.LatestMessageProperties.BuildingId);
        }

        Task<string> IBuildingSensorActor.GetDeviceId()
        {
            return Task.FromResult(this.State.LatestMessageProperties.DeviceId);
        }

        Task<double> IBuildingSensorActor.GetHumityPercentage()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Humidity);
        }

        Task<bool> IBuildingSensorActor.GetLightStatus()
        {
            return Task.FromResult(this.State.LatestMessageProperties.Light);
        }

        Task<double> IBuildingSensorActor.GetTemperatureInFahrenheit()
        {
            return Task.FromResult(this.State.LatestMessageProperties.TempF);
        }

        Task<DateTime> IBuildingSensorActor.GetLastMessageTime()
        {
            return Task.FromResult(this.State.LatestMessageTime);
        }

    }
}
