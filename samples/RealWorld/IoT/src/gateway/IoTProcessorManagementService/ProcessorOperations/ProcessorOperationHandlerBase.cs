using IoTProcessorManagement.Clients;
using IoTProcessorManagement.Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    public abstract class ProcessorOperationHandlerBase
    {
        protected ProcessorManagementService Svc;
        protected ProcessorOperation processorOperation;
        public ProcessorOperationHandlerBase(ProcessorManagementService svc, ProcessorOperation Operation)
        {
            Svc = svc;
            processorOperation = Operation;
        }

        protected async Task UpdateProcessorAsync(Processor processor, ITransaction tx = null, bool CommitInNewTransaction = false, bool OverwriteServiceFabricnames = false)
        {
            var _trx = CommitInNewTransaction ? Svc.StateManager.CreateTransaction() : tx;

            if (null == _trx)
                throw new InvalidOperationException("Save processor need a transaction to work with if it is not commitable");


            await Svc.ProcessorStateStore.AddOrUpdateAsync(_trx,
                                            processor.Name,
                                            processor,
                                            (name, proc) =>
                                            {
                                                proc.SafeUpdate(processor, OverwriteServiceFabricnames);
                                                return proc;
                                            });

            if (CommitInNewTransaction)
            {
                await _trx.CommitAsync();
                _trx.Dispose();
            }


            ServiceEventSource.Current.Message(string.Format("processor {0} Updated with tx:{1} NewCommit:{2} OverwriteNames:{3}", processor.Name, tx ==null, CommitInNewTransaction, OverwriteServiceFabricnames));
        }
        protected async Task<Processor> GetProcessorAsync(string ProcessorName, ITransaction tx = null)
        {
            var _trx = tx ?? Svc.StateManager.CreateTransaction();


            Processor processor;
            var cResult = await Svc.ProcessorStateStore.TryGetValueAsync(_trx, ProcessorName);
            if (cResult.HasValue)
                processor = cResult.Value;
            else
                processor = null;

            if (null == tx)
            {
                await _trx.CommitAsync();
                _trx.Dispose();
            }

            return processor;
        }

        protected async Task<bool> ReEnqueAsync(ITransaction tx)
        {
            processorOperation.RetryCount++;

            if (processorOperation.RetryCount > ProcessorManagementService.s_MaxProcessorOpeartionRetry)
                return false;

            await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, processorOperation);
            return true;
        }

        private async Task<IList<ServicePartitionClient<ProcessorServiceCommunicationClient>>> GetServicePartitionClientsAsync(string ServiceName, 
                                                                                                                            int MaxQueryRetryCount = 5, 
                                                                                                                            int BackOffRetryDelaySec = 3)
        {
            for (int i = 0; i < MaxQueryRetryCount; i++)
            {
                try
                {
                    FabricClient fabricClient = new FabricClient();

                    // Get the list of partitions up and running in the service.
                    ServicePartitionList partitionList = await fabricClient.QueryManager.GetPartitionListAsync(new Uri(ServiceName));

                    // For each partition, build a service partition client used to resolve the low key served by the partition.
                    IList<ServicePartitionClient<ProcessorServiceCommunicationClient>> partitionClients =
                        new List<ServicePartitionClient<ProcessorServiceCommunicationClient>>(partitionList.Count);
                    foreach (Partition partition in partitionList)
                    {
                        Int64RangePartitionInformation partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;
                        partitionClients.Add(
                            new ServicePartitionClient<ProcessorServiceCommunicationClient>(Svc.m_ProcessorServiceCommunicationClientFactory, new Uri(ServiceName), partitionInfo.LowKey));
                    }

                    return partitionClients;
                }
                catch (FabricTransientException ex)
                {

                    if (i == MaxQueryRetryCount - 1)
                    {
                        ServiceEventSource.Current.ServiceMessage(Svc, "Processor Operation Handler failed to resolve service partition after:{0} retry with backoff retry:{1} E:{2} Stack Trace:{3}",
                                                                    MaxQueryRetryCount, BackOffRetryDelaySec, ex.Message, ex.StackTrace);
                        throw;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(BackOffRetryDelaySec).Milliseconds);
            }

            throw new TimeoutException("Retry timeout is exhausted and creating representative partition clients wasn't successful");
        }


        private Task<HttpRequestMessage> cloneHttpRequestMesageAsync(HttpRequestMessage Source)
        {
            // poor man's http method cloner
            // ding, ding, ding! if the content was previoulsy consumed 

            HttpRequestMessage copy = new HttpRequestMessage(Source.Method, Source.RequestUri);

            copy.Version = Source.Version;

            foreach (var p in Source.Properties)
                copy.Properties.Add(p);
            

            foreach (var requestHeader in Source.Headers)
                copy.Headers.TryAddWithoutValidation(requestHeader.Key, requestHeader.Value);

            if (Source.Method != HttpMethod.Get)
                copy.Content = Source.Content;


            return Task.FromResult(copy);

        }

        protected Task<HttpRequestMessage> GetBasicPartitionHttpRequestMessageAsync()
        {
            // if you want to add default heads such as AuthN, add'em here. 

            return Task.FromResult(new HttpRequestMessage());

        } 
        protected async Task<Task<HttpResponseMessage>[]> SendHttpAllServicePartitionAsync(string ServiceName, HttpRequestMessage Message, string requestPath) 
        {


            // Get the list of representative service partition clients.
            IList<ServicePartitionClient<ProcessorServiceCommunicationClient>> partitionClients = await this.GetServicePartitionClientsAsync(ServiceName);

            IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>(partitionClients.Count);
            foreach (ServicePartitionClient<ProcessorServiceCommunicationClient> partitionClient in partitionClients)
            {
                var message = await cloneHttpRequestMesageAsync(Message);
                

                // partitionClient internally resolves the address and retries on transient errors based on the configured retry policy.
                tasks.Add(
                    partitionClient.InvokeWithRetryAsync(
                        client =>
                        {
                            message.RequestUri = new Uri(string.Concat(client.BaseAddress, requestPath)); 
                            HttpClient httpclient = new HttpClient();
                            return httpclient.SendAsync(message);

                        }));

            }

            return tasks.ToArray();
        }


        #region Service Fabric Application & Services Management 
        protected async Task CleanUpServiceFabricCluster(Processor processor)
        {
            try
            {
                await DeleteServiceAsync(processor);
            }
            catch(AggregateException ae)
            {
                ServiceEventSource.Current.ServiceMessage(Svc, "Delete Service for processor:{0} service:{1} failed, will keep working normally E:{2} StackTrace:{3}", processor.Name, processor.ServiceFabricServiceName, ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace());
            }


            try
            {
                await DeleteAppAsync(processor);
            }
            catch (AggregateException ae)
            {
                ServiceEventSource.Current.ServiceMessage(Svc, "Delete App for processor:{0} app:{1} failed, will keep working normally E:{2} StackTrace:{3}", processor.Name, processor.ServiceFabricAppInstanceName, ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace());
            }
        }


        protected async Task DeleteServiceAsync(Processor processor)
        {
            var sServiceName = new Uri(processor.ServiceFabricServiceName);

            FabricClient fabricClient = new FabricClient();
            await fabricClient.ServiceManager.DeleteServiceAsync(sServiceName);

            ServiceEventSource.Current.ServiceMessage(Svc, "Service for processor:{0} service:{1} deleted.", processor.Name, processor.ServiceFabricServiceName);


        }

        protected async Task DeleteAppAsync(Processor processor)
        {
            FabricClient fabricClient = new FabricClient();
            await fabricClient.ApplicationManager.DeleteApplicationAsync(new Uri(processor.ServiceFabricAppInstanceName));
            ServiceEventSource.Current.ServiceMessage(Svc, "App for processor:{0} app:{1} deleted", processor.Name, processor.ServiceFabricAppInstanceName);

        }


        protected async Task CreateAppAsync(Processor processor)
        {

            FabricClient fabricClient = new FabricClient();
            ApplicationDescription appDesc = new ApplicationDescription(new Uri(processor.ServiceFabricAppInstanceName),
                                                                         processor.ServiceFabricAppTypeName,
                                                                         processor.ServiceFabricAppTypeVersion);


            // create the app
            await fabricClient.ApplicationManager.CreateApplicationAsync(appDesc);
            ServiceEventSource.Current.ServiceMessage(Svc, "App for processor:{0} app:{1} created", processor.Name, processor.ServiceFabricAppInstanceName);

        }

        protected async Task CreateServiceAsync(Processor processor)
        {

            FabricClient fabricClient = new FabricClient();
            await fabricClient.ServiceManager.CreateServiceFromTemplateAsync(
                                        new Uri(processor.ServiceFabricAppInstanceName),
                                        new Uri(processor.ServiceFabricServiceName),
                                        Svc.DefaultProcessorAppInstanceDefinition.ServiceTypeName,
                                        processor.AsBytes()
                        );


            ServiceEventSource.Current.ServiceMessage(Svc, "Service for processor:{0} service:{1} created.", processor.Name, processor.ServiceFabricServiceName);


        }
        #endregion

        public abstract Task RunOperation( ITransaction tx);

        public abstract Task<T> ExecuteOperation<T>(ITransaction tx) where T : class;    
    }
}
