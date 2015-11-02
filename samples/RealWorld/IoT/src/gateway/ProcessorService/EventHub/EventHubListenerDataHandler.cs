﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace EventHubProcessor
{
    using System.Threading.Tasks;
    using IoTProcessorManagement.Common;
    using Microsoft.ServiceBus.Messaging;

    internal class EventHubListenerDataHandler : IEventDataHandler
    {
        private WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> m_WorkManager;

        public EventHubListenerDataHandler(WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> WorkManager)
        {
            this.m_WorkManager = WorkManager;
        }


        public async Task HandleEventData(string ServiceBusNamespace, string EventHub, string ConsumerGroupName, EventData ed)
        {
            RouteToActorWorkItem wi = await RouteToActorWorkItem.CreateAsync(
                ed,
                ed.SystemProperties[EventDataSystemPropertyNames.Publisher].ToString(),
                EventHub,
                ServiceBusNamespace);

            await this.m_WorkManager.PostWorkItemAsync(wi);
        }
    }
}