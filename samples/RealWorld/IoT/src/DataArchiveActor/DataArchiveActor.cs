using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataArchiveActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DataArchiveActor
{
    public class DataArchiveActor : Actor<DataArchiveActorState>, IDataArchiveActor
    {
        private const int BatchSize = 1;
        private const string SensorsTableName = "sensors";

        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new DataArchiveActorState()
                {
                    SensorMessages = new List<SensorMessage>()
                };
            }

            ActorEventSource.Current.ActorMessage(this, "State initialized to {0}", this.State);
            return Task.FromResult(true);
        }

        public Task SaveDeviceStateAsync(SensorMessage message)
        {
            this.State.SensorMessages.Add(message);
            if (this.State.SensorMessages.Count >= BatchSize)
                return writeToAzureStorage();
            return Task.FromResult(true);
        }
        private async Task writeToAzureStorage()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(SensorsTableName);
            table.CreateIfNotExists();

            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach(var message in this.State.SensorMessages)
            {
                var entity = new SensorDataEntity(message);
                batchOperation.InsertOrReplace(entity);
            }

            await table.ExecuteBatchAsync(batchOperation);

            this.State.SensorMessages.Clear();
        }
    }
}
