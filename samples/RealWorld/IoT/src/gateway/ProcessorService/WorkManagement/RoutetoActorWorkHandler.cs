using IoTProcessorManagement.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerBIActor.Interfaces;
using Microsoft.ServiceFabric.Actors;

namespace EventHubProcessor
{
    public class RoutetoActorWorkItemHandler : IWorkItemHandler<RouteToActorWorkItem>
    {
        //todo: Each handler will a reference to Device Actor Proxy
        // Each handler is assigned to a queue (and queue is assigned to device). 

        private IPowerBIActor m_actor = null;
        private object actor_lock = new object();
        private IPowerBIActor getActor(RouteToActorWorkItem wi)
        {
            if (m_actor != null)
                return m_actor;

            lock (actor_lock)
            {
                if (m_actor != null)
                    return m_actor;

                
                ActorId id = new ActorId(wi.QueueName);


                m_actor = ActorProxy.Create<IPowerBIActor>(id, new Uri("fabric:/IoTApplication/PowerBIActorService"));

                return m_actor;
            }

        }

        public async Task<RouteToActorWorkItem> HandleWorkItem(RouteToActorWorkItem wi)
        {
            await Task.Delay(100);
            return null;
            /*
            IPowerBIActor actor = getActor(wi);
            await actor.Post(wi.PublisherName, wi.EventHubName, wi.ServiceBusNS, wi.Body);

            return null; // if a wi is returned, it signals the work manager to re-reenqueu
        */
            }
    }
}
