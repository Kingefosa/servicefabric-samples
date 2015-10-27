// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace DeviceActor
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using IoTActor.Common;
    using Microsoft.ServiceFabric.Actors;
    using Newtonsoft.Json.Linq;

    public class DeviceActor : Actor<DeviceActorState>, IIoTActor
    {
        private static string s_FloorActorService = "fabric:/IoTApplication/FloorActor";
        private static string s_FloorActorIdFormat = "{0}-{1}-{2}";

        private static string s_StorageActorService = "fabric:/IoTApplication/StorageActor";
        private static string s_StorageActorIdFormat = "{0}-{1}-{2}";


        private IIoTActor m_FloorActor = null;
        private IIoTActor m_StorageActor = null;

        public async Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            Task TaskFloorForward = this.ForwardToNextAggregator(DeviceId, EventHubName, ServiceBusNS, Body);
            Task TaskStorageForward = this.ForwardToStorageActor(DeviceId, EventHubName, ServiceBusNS, Body);

            /*
            While we are waiting for the next actor in chain the device actor can do CEP to identify
            if a an action is needed on the device. if so it can aquire a channel to the device it self 
            and send the command. 
            */

            await Task.WhenAll(TaskFloorForward, TaskStorageForward);
        }

        public override Task OnActivateAsync()
        {
            return Task.FromResult(true);
        }

        private IIoTActor CreateFloorActor(string DeviceId, string FloorId, string EventHubName, string ServiceBusNS)
        {
            ActorId actorId = new ActorId(string.Format(s_FloorActorIdFormat, FloorId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(s_FloorActorService));
        }

        private IIoTActor CreateStorageActor(string DeviceId, string EventHubName, string ServiceBusNS)
        {
            ActorId actorId = new ActorId(string.Format(s_StorageActorIdFormat, DeviceId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(s_StorageActorService));
        }

        private async Task ForwardToNextAggregator(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == this.m_FloorActor)
            {
                JObject j = JObject.Parse(Encoding.UTF8.GetString(Body));
                string FloorId = j["FloorId"].Value<string>();

                this.m_FloorActor = this.CreateFloorActor(DeviceId, FloorId, EventHubName, ServiceBusNS);
            }
            await this.m_FloorActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }

        private async Task ForwardToStorageActor(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == this.m_StorageActor)
                this.m_StorageActor = this.CreateStorageActor(DeviceId, EventHubName, ServiceBusNS);

            await this.m_StorageActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }
    }
}