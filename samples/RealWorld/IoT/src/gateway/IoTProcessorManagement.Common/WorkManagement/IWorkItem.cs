using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IoTProcessorManagement.Common;

namespace IoTProcessorManagement.Common
{
    public interface IWorkItem 
    {
        string QueueName { get; }
    }
  
}
