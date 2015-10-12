using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// vanila implementation of IEventProcessorFactory (refer to Event Hub Sdk).
    /// </summary>
    class EventProcessorFactory : IEventProcessorFactory
    {
        private IEventDataHandler m_Handler;
        private string m_EventHubName;
        private string m_ServiceBusNamespace;
        private string m_CounsumerGroupName;
        public EventProcessorFactory(IEventDataHandler Handler, string EventHubName, string ServiceBusNamespace, string ConsumerGroupName)
        {
            m_Handler = Handler;
            m_EventHubName = EventHubName;
            m_ServiceBusNamespace = ServiceBusNamespace;
            m_CounsumerGroupName = ConsumerGroupName;
        }
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(m_Handler, m_EventHubName, m_ServiceBusNamespace, m_CounsumerGroupName);
        }
    }
}
