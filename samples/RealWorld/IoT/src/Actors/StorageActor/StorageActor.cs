// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using IoTActor.Common;

namespace StorageActor
{
    [ActorGarbageCollection(IdleTimeoutInSeconds = 60, ScanIntervalInSeconds = 10)]
    public class StorageActor : Actor<StorageActorState>, IIoTActor
    {
        private static int s_MaxEntriesPerRound = 100;
        static string s_PowerBIActorServiceName = "fabric:/IoTApplication/PowerBIActor";
        static string s_PowerBIActorId = "{0}-{1}-{2}";


        private IActorTimer m_DequeueTimer = null;
        private string m_TableName = string.Empty;
        private string m_ConnectionString = string.Empty;
        private IIoTActor m_PowerBIActor = null;


 #region Send To PowerBI Actor

        private IIoTActor CreatePowerBIActor(string DeviceId, string EventHubName, string ServiceBusNS)
        {
            var actorId = new ActorId(string.Format(s_PowerBIActorId, DeviceId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(s_PowerBIActorServiceName));
        }
        private async Task ForwardToPowerBIActor(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == m_PowerBIActor)
                m_PowerBIActor = CreatePowerBIActor(DeviceId , EventHubName, ServiceBusNS);

            await m_PowerBIActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }

#endregion

        #region Config Management
        private void ConfigChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            SetConfig().Wait();
        }
        private  Task SetConfig()
        {
            var settingsFile = Host.ActivationContext.GetConfigurationPackageObject("Config").Settings;
            var configSection = settingsFile.Sections["Storage"];

            m_TableName = configSection.Parameters["TableName"].Value;
            m_ConnectionString = configSection.Parameters["ConnectionString"].Value;

            return Task.FromResult(0);
        }
        #endregion

     
        #region Save Logic
        private async Task SaveToStorage(object IsFinal)
        {
            if (0 == State.Queue.Count)
                return;

            
            var bFinal = (bool)IsFinal; // as in actor instance is about to get deactivated. 
            var nCurrent = 0;
            
            

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(m_ConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(m_TableName);
            table.CreateIfNotExists();

            TableBatchOperation batchOperation = new TableBatchOperation();

            while ((nCurrent <= s_MaxEntriesPerRound || bFinal) && (0 != State.Queue.Count))
                batchOperation.InsertOrReplace(State.Queue.Dequeue().ToDynamicTableEntity());

            await table.ExecuteBatchAsync(batchOperation);


        }
        #endregion


     
        public override async Task OnActivateAsync()
        {
            if (this.State == null)
                this.State = new StorageActorState();



            await SetConfig();
            Host.ActivationContext.ConfigurationPackageModifiedEvent += ConfigChanged;


            // register a call back timer, that perfoms the actual send to PowerBI
            // has to iterate in less than IdleTimeout 
            m_DequeueTimer = RegisterTimer(
                                            SaveToStorage,
                                            false,
                                            TimeSpan.FromMilliseconds(8),
                                            TimeSpan.FromMilliseconds(8));

            await base.OnActivateAsync();
        }
        public override async Task OnDeactivateAsync()
        {
            UnregisterTimer(m_DequeueTimer); // remove the actor timer
            await SaveToStorage(true); // make sure that no remaining pending records 
            await base.OnDeactivateAsync();
        }

        public async Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            var TaskForward = ForwardToPowerBIActor(DeviceId, EventHubName, ServiceBusNS, Body);

            var taskAdd =  Task.Run(() =>
                            {
                                var Wi = new IoTActorWorkItem();
                                Wi.DeviceId = DeviceId;
                                Wi.EventHubName = EventHubName;
                                Wi.ServiceBusNS = ServiceBusNS;
                                Wi.Body = Body;

                                State.Queue.Enqueue(Wi);
                            }
                      );

            await Task.WhenAll(TaskForward, taskAdd);
        }

     
    }
}
