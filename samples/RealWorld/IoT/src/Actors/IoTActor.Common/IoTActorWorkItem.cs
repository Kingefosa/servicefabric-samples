// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTActor.Common
{
    public class IoTActorWorkItem
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

        public  DynamicTableEntity ToDynamicTableEntity()
        {
            var entity = new DynamicTableEntity();
            var j = toJObject();

            entity.PartitionKey = string.Format("{0}-{1}-{2}",
                j.Value<string>("DeviceId"),
                j.Value<string>("EventHubName"),
                j.Value<string>("ServiceBusNS")
                );
            entity.RowKey = DateTime.UtcNow.Ticks.ToString();
            foreach (var t in j)
                entity.Properties.Add(t.Key, new EntityProperty(t.Value.ToString()));
                
          
            return entity;

        }
    }
}
