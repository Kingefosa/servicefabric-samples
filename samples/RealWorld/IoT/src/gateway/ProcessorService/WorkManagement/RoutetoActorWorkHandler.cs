// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace EventHubProcessor
{
    using System;
    using System.Threading.Tasks;
    using IoTActor.Common;
    using IoTProcessorManagement.Common;
    using Microsoft.ServiceFabric.Actors;

    public class RoutetoActorWorkItemHandler : IWorkItemHandler<RouteToActorWorkItem>
    {
        // Each handler is assigned to a queue (and queue is assigned to device). 
        private static string s_DeviceActorServiceName = "fabric:/IoTApplication/DeviceActor";


        private IIoTActor m_DeviceActor = null;
        private object actor_lock = new object();

        public async Task<RouteToActorWorkItem> HandleWorkItem(RouteToActorWorkItem wi)
        {
            IIoTActor DeviceActor = this.getActor(wi);
            await DeviceActor.Post(wi.PublisherName, wi.EventHubName, wi.ServiceBusNS, wi.Body);


            return null; // if a wi is returned, it signals the work manager to re-enqueu
        }

        private IIoTActor getActor(RouteToActorWorkItem wi)
        {
            if (this.m_DeviceActor != null)
                return this.m_DeviceActor;

            lock (this.actor_lock)
            {
                if (this.m_DeviceActor != null)
                    return this.m_DeviceActor;

                ActorId id = new ActorId(wi.QueueName);
                this.m_DeviceActor = ActorProxy.Create<IIoTActor>(id, new Uri(s_DeviceActorServiceName));
                return this.m_DeviceActor;
            }
        }
    }
}