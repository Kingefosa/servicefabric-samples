using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIActor
{
    public class PowerBIWorkItem
    {
        public string DeviceId { get; set; } = string.Empty;
        public string EventHubName { get; set; } = string.Empty;
        public string ServiceBusNS { get; set; } = string.Empty;
        public byte[] Body { get; set; }

        public JObject toJObject()
        {
            var j = JObject.Parse(Encoding.UTF8.GetString(Body));
            j.Add("EventHubName", EventHubName);
            j.Add("ServiceBusNS", ServiceBusNS);

            return j;
        }
    }
}
