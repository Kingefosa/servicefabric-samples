using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// defines the tracing interface between the 
    /// service and an external component (which does not live in the same assembly)
    /// </summary>
    public interface ITraceWriter
    {
        void TraceMessage(string message);
    }
}
