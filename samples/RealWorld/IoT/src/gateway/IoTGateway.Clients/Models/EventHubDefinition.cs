using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Clients
{
    public class EventHubDefinition
    {
        public string ConnectionString { get; set; }
        public string EventHubName { get; set; }
        public string ConsumerGroupName { get; set; }
    }
}
