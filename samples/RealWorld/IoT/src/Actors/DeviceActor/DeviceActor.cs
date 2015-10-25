using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using System.Text;
using IoTActor.Common;
using Newtonsoft.Json.Linq;

namespace DeviceActor
{
    public class DeviceActor : Actor<DeviceActorState>, IIoTActor
    {
        static string s_FloorActorService = "fabric:/IoTApplication/FloorActor";
        static string s_FloorActorIdFormat = "{0}-{1}-{2}";

        static string s_StorageActorService = "fabric:/IoTApplication/StorageActor";
        static string s_StorageActorIdFormat = "{0}-{1}-{2}";


        private IIoTActor m_FloorActor = null;
        private IIoTActor m_StorageActor = null;
        
        private IIoTActor CreateFloorActor(string DeviceId, string FloorId, string EventHubName, string ServiceBusNS)
        {
            var  actorId = new ActorId(string.Format(s_FloorActorIdFormat, FloorId, EventHubName, ServiceBusNS));
            return  ActorProxy.Create<IIoTActor>(actorId, new Uri(s_FloorActorService));
        }
        private IIoTActor CreateStorageActor(string DeviceId,  string EventHubName, string ServiceBusNS)
        {
            var actorId = new ActorId(string.Format(s_StorageActorIdFormat, DeviceId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(s_StorageActorService));
        }
        private async Task ForwardToNextAggregator(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == m_FloorActor)
            {
                var j = JObject.Parse(Encoding.UTF8.GetString(Body));
                var FloorId = j["FloorId"].Value<string>();

                m_FloorActor = CreateFloorActor(DeviceId, FloorId, EventHubName, ServiceBusNS);
            }
            await m_FloorActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }
        private async Task ForwardToStorageActor(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == m_StorageActor)
                m_StorageActor = CreateStorageActor(DeviceId, EventHubName, ServiceBusNS);

            await m_StorageActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }
        public override Task OnActivateAsync()
        {
            return Task.FromResult(true);
        }

        public async Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            var TaskFloorForward = ForwardToNextAggregator(DeviceId, EventHubName, ServiceBusNS, Body);
            var TaskStorageForward = ForwardToStorageActor(DeviceId, EventHubName, ServiceBusNS, Body);

            /*
            While we are waiting for the next actor in chain the device actor can do CEP to identify
            if a an action is needed on the device. if so it can aquire a channel to the device it self 
            and send the command. 
            */

            await Task.WhenAll(TaskFloorForward, TaskStorageForward);
        }
    }
}
