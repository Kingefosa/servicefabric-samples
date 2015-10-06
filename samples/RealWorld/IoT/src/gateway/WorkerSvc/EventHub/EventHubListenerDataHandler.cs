using IoTGateway.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace WorkerSvc
{
    class EventHubListenerDataHandler : IEventDataHandler
    {
        private WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> m_WorkManager;
        public EventHubListenerDataHandler(WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> WorkManager)
        {
            m_WorkManager = WorkManager;
        }
        



        public async Task HandleEventData(string ServiceBusNamespace, string EventHub, string ConsumerGroupName, EventData ed)
        {
            RouteToActorWorkItem wi = await RouteToActorWorkItem.CreateAsync(ed,
                                                                             ed.SystemProperties[EventDataSystemPropertyNames.Publisher].ToString(),
                                                                             EventHub,
                                                                             ServiceBusNamespace);

            await m_WorkManager.PostWorkItemAsync(wi);


        }
    }
}
