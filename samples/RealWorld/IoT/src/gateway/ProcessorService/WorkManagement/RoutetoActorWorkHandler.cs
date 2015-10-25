using IoTProcessorManagement.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric.Actors;
using IoTActor.Common;

namespace EventHubProcessor
{
    public class RoutetoActorWorkItemHandler : IWorkItemHandler<RouteToActorWorkItem>
    {
      
        // Each handler is assigned to a queue (and queue is assigned to device). 
        private static string s_DeviceActorServiceName = "fabric:/IoTApplication/DeviceActor";
        
        
        private IIoTActor m_DeviceActor = null;
        private object actor_lock = new object();
        
        private IIoTActor getActor(RouteToActorWorkItem wi)
        {
            
            if (m_DeviceActor != null)
                return m_DeviceActor;

            lock (actor_lock)
            {
                if (m_DeviceActor != null)
                    return m_DeviceActor;

                ActorId id = new ActorId(wi.QueueName);
                m_DeviceActor = ActorProxy.Create<IIoTActor>(id, new Uri(s_DeviceActorServiceName));
                return m_DeviceActor;
            }

        }
       
        public async Task<RouteToActorWorkItem> HandleWorkItem(RouteToActorWorkItem wi)
        {
                IIoTActor DeviceActor = getActor(wi);
                await DeviceActor.Post(wi.PublisherName, wi.EventHubName, wi.ServiceBusNS, wi.Body);

                

            return null; // if a wi is returned, it signals the work manager to re-enqueu
        }
    }
}
