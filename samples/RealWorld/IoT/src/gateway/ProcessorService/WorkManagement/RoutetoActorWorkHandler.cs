using IoTProcessorManagement.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubProcessor
{
    public class RoutetoActorWorkItemHandler : IWorkItemHandler<RouteToActorWorkItem>
    {
        //todo: Each handler will a reference to Device Actor Proxy
        // Each handler is assigned to a queue (and queue is assigned to device). 
        public async Task<RouteToActorWorkItem> HandleWorkItem(RouteToActorWorkItem wi)
        {
            await Task.Delay(200);

            return null; // if a wi is returned, it signals the work manager to re-enqueu
        }
    }
}
