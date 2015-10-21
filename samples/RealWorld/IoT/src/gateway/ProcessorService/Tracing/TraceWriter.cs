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
        public EventHubProcessorService Svc { get; private set; }

        public TraceWriter(EventHubProcessorService svc)
        {
            Svc = svc;
        }
        public void TraceMessage(string message)
        {
            var assignedProcessor = Svc.GetAssignedProcessorAsync().Result;
            var prefix = "";

            if (null != assignedProcessor)
                prefix = string.Format("Assigned Processor Name:{0}", assignedProcessor.Name);

            ServiceEventSource.Current.ServiceMessage(Svc,  string.Concat(prefix, "\n", message));
        }
    }
}
