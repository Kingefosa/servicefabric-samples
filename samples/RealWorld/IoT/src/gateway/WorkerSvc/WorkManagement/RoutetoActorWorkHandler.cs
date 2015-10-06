using IoTGateway.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerSvc
{
    public class RoutetoActorWorkItemHandler : IWorkItemHandler<RouteToActorWorkItem>
    {
        public async Task<RouteToActorWorkItem> HandleWorkItem(RouteToActorWorkItem wi)
        {
            //   Trace.WriteLine(string.Format("got {0} by publisher {1} on Event Hub {2}", Encoding.UTF8.GetString(wi.Body), wi.PublisherName, wi.EventHubName));
            // await Task.Delay(200);

            return null; // if a wi is returned, it signals the work manager to re-enqueu
        }
    }
}
