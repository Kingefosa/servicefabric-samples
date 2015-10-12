using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// vanila implementation of IEventProcessor (refer to Event Hub SDK).
    /// the only difference, is we presiste lease (the cursor) with ever event data. 
    /// </summary>
    class EventProcessor : IEventProcessor
    {
        private IEventDataHandler m_Handler;
        private string m_EventHubName;
        private string m_ServiceBusNamespace;
        private string m_CounsumerGroupName;


        public EventProcessor(IEventDataHandler Handler, string EventHubName, string ServiceBusNamespace, string ConsumerGroupName)
        {
            m_Handler = Handler;
            m_EventHubName = EventHubName;
            m_ServiceBusNamespace = ServiceBusNamespace;
            m_CounsumerGroupName = ConsumerGroupName;
        }
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            // no op
            return Task.FromResult(0);
        }

        public Task OpenAsync(PartitionContext context)
        {
            // no op
            return Task.FromResult(0);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var ev in messages)
            {
                await m_Handler.HandleEventData(m_ServiceBusNamespace, m_EventHubName, m_CounsumerGroupName ,ev);
                await context.CheckpointAsync();
            }
            
        }
    }
}
