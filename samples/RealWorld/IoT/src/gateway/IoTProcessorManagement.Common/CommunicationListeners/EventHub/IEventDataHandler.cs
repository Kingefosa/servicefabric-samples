// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// represents the handler that will accept event data
    /// as they are pumped out from an event hub partition.
    /// </summary>
    public interface IEventDataHandler
    {
        Task HandleEventData(string ServiceBusNamespace, string EventHub, string ConsumerGroupName, EventData ed);
    }
}
