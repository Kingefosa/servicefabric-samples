using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SensorActor.Interfaces;
using SensorActor.Common;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using System.Text;
using FloorActor.Interfaces;
using DataArchiveActor.Interfaces;

namespace SensorActor
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

        Task ISensorActor.SendDeviceStateAsync(DateTime timeOfEvent, byte[] messageBody)
        {
            this.State.LatestMessageTime = timeOfEvent;
            this.State.LatestMessageProperties = JsonConvert.DeserializeObject<SensorMessage>(Encoding.UTF8.GetString(messageBody));
            if (this.State.LatestMessageProperties != null
                && !string.IsNullOrEmpty(this.State.LatestMessageProperties.FloorId)
                && !string.IsNullOrEmpty(this.State.LatestMessageProperties.DeviceId))
            {
                var floor = ActorProxy.Create<IFloorActor>(new ActorId(State.LatestMessageProperties.FloorId));
                return floor.SendDeviceStateAsync(this.State.LatestMessageProperties);
            }
            return Task.FromResult(true);
        }

        Task<SensorMessage> ISensorActor.GetLastMessageAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties);
        }

        Task<string> ISensorActor.GetBuildingIdAsync()
        {
            return Task.FromResult(this.State.LatestMessageProperties.FloorId);
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
