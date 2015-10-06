using IoTGateway.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerSvc
{
    
    public class RouteToActorWorkItem : IWorkItem
    {
        private static readonly string s_QueueName_Format = "{0}";

        public RouteToActorWorkItem()
        {

        }

        public static async Task<RouteToActorWorkItem> CreateAsync(EventData ev, string publisherName, string eventHubName, string serviceBusNS)
        {
            var wi = new RouteToActorWorkItem()
            {
                Body = await ev.GetBodyStream().ToBytes(),
                PublisherName = publisherName,
                EventHubName = eventHubName,
                ServiceBusNS = serviceBusNS
            };

            return wi;
        }

        // TODO: ignore serializable
        public string QueueName
        {
            get
            {
                return string.Format(RouteToActorWorkItem.s_QueueName_Format,
                                     this.PublisherName);
            }
        }

        public string PublisherName { get; set; }
        public string EventHubName { get; set; }
        public string ServiceBusNS { get; set; }
        public byte[] Body { get; set; }
    }
}
