﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using IoTActor.Common;
using System.Text;
using Newtonsoft.Json.Linq;


namespace FloorActor
{
    public class FloorActor : Actor<FloorActorState>, IIoTActor
    {
        static string s_BuildingActorService = "fabric:/IoTApplication/BuildingActor";
        static string s_BuildingActorIdFormat = "{0}-{1}-{2}";

        private IIoTActor m_BuildingActor = null;


        private IIoTActor CreateBuildingActor(string BuildingId, string EventHubName, string ServiceBusNS)
        {
            var actorId = new ActorId(string.Format(s_BuildingActorIdFormat, BuildingId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(s_BuildingActorService));
        }
        private async Task ForwardToBuildingActor(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == m_BuildingActor)
            {
                var j = JObject.Parse(Encoding.UTF8.GetString(Body));
                var BuildingId = j["BuildingId"].Value<string>();

                m_BuildingActor = CreateBuildingActor(BuildingId, EventHubName, ServiceBusNS);
            }
            await m_BuildingActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }


        public async  Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
 
            var taskForward = ForwardToBuildingActor(DeviceId, EventHubName, ServiceBusNS, Body);

            /*
           The following are the chain in this samples
           Device->Floor->Building

           You can tailor the aggregators even further. for example
           Device->Floor->Building->Campus

           A floor actor in this sample represents end of the chain, you can use it
           to respond to events at the floor level (including all devices). 

           Note: aggregators should not store events, since raw events are stored
                 at the device level using Storage Actor. You can choose to store other 
                 data such as commands generated by floor actor.. 
           */

            // mean while you can do CEP to generate commands to devices. 

            await taskForward;
        }
    }
}
