using IoTProcessorManagement.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubProcessor
{
    public class TraceWriter : ITraceWriter
    {
        public bool EnablePrefix = false; 
        public IoTEventHubProcessorService Svc { get; private set; }

        public TraceWriter(IoTEventHubProcessorService svc)
        {
            Svc = svc;
        }
        public void TraceMessage(string message)
        {
            var prefix = "";

            if (EnablePrefix)
            { 
            var assignedProcessor = Svc.GetAssignedProcessorAsync().Result;
            
            if (null != assignedProcessor)
                prefix = string.Format("Assigned Processor Name:{0}", assignedProcessor.Name);
            }
            ServiceEventSource.Current.ServiceMessage(Svc,  string.Concat(prefix, "\n", message));
        }
    }
}
