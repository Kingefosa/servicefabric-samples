using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IoTGateway.Common;

namespace IoTGateway.Common
{
    public interface IWorkItem 
    {
        string QueueName { get; }
    }
  
}
