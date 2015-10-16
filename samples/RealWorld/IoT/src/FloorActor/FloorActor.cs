using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloorActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace FloorActor
{
    public class FloorActor : Actor<FloorActorState>, IFloorActor
    {
        private IActorTimer m_DequeueTimer = null;
        private TimeSpan m_TimeSpanToKeepMessages = new TimeSpan(1, 0, 0);
        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new FloorActorState() { Messages = new List<SensorMessage>() };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            m_DequeueTimer = RegisterTimer(
                                                    CleanupMessageList,
                                                    null,
                                                    TimeSpan.FromMinutes(30),
                                                    TimeSpan.FromMinutes(30));


            return Task.FromResult(true);
        }

        public override Task OnDeactivateAsync()
        {
            UnregisterTimer(m_DequeueTimer); // remove the actor timer

            return base.OnDeactivateAsync();
        }

        Task IFloorActor.ReceiveMessageAsync(SensorMessage m)
        {
            State.Messages.Add(m);

            return Task.FromResult(true);
        }

        async Task<double> IFloorActor.GetAverageHumidityForLastMinuteAsync()
        {
            IEnumerable<SensorMessage> messages =
                await GetMessagesForTimeSpanAsync(TimeSpan.FromMinutes(1));
            double average = messages.Select((m) => m.Humidity).Average();
            return average;
        }

        async Task<double> IFloorActor.GetAverageTemperatureForLastMinuteAsync()
        {
            IEnumerable<SensorMessage> messages =
                await GetMessagesForTimeSpanAsync(TimeSpan.FromMinutes(1));
            double average = messages.Select((m) => m.TempF).Average();
            return average;
        }

        Task<IEnumerable<SensorMessage>> GetMessagesForTimeSpanAsync(TimeSpan ts)
        {
            DateTime t = DateTime.Now - ts;
            IEnumerable<SensorMessage> messages = State.Messages.Where((m) => m.Time >= t);
            return Task.FromResult(messages);
        }

        Task<string> IFloorActor.GetFloorIdAsync()
        {
            return Task.FromResult(this.GetActorId().ToString());
        }

        private async Task CleanupMessageList(object IsFinal)
        {
            this.State.Messages = (await GetMessagesForTimeSpanAsync(m_TimeSpanToKeepMessages)).ToList();
        }
    }
}
