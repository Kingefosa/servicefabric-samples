using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public interface IEventDataHandler
    {
        // returns bool to ensure that checkpoint is saved 
        Task HandleEventData(string ServiceBusNamespace, string EventHub, string ConsumerGroupName, EventData ed);
    }
}
