using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubProcessor
{
    // Each Web API controller will implement this interface
    // the Owin pipeline assigns a dependancy resolver to inject
    // each controller with a service reference. 
    public interface IEventHubProcessorController
    {
         EventHubProcessorService ProcessorService { get; set; }
    }
}
