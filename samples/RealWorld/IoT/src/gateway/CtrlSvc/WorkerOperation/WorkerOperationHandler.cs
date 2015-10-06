using IoTGateway.Clients;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrlSvc
{
    abstract class WorkerOpeartionHandler
    {
        protected IReliableStateManager StateManager;
        protected WorkerOperation WorkerOperation;
        public WorkerOpeartionHandler(IReliableStateManager stateManager, WorkerOperation Operation)
        {
            StateManager = stateManager;
            WorkerOperation = Operation;
        }

        protected async Task SaveWorkerAsync(Worker worker, ITransaction tx = null, bool CommitInNewTransaction = false)
        {
            var _trx = CommitInNewTransaction ? StateManager.CreateTransaction() : tx;

            if (null == _trx)
                throw new InvalidOperationException("Save worker need a transaction to work with if it is not commitable");

            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
            // this a brute force update.    
            await workers.AddOrUpdateAsync(_trx, worker.Name, worker, (name, wrk) => { return worker; });

            if (CommitInNewTransaction)
            {
                await _trx.CommitAsync();
                _trx.Dispose();
            }
          }
        protected async Task<Worker> GetWorkerAsync(string WorkerName, ITransaction tx)
        {
            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
            var cResult = await workers.TryGetValueAsync(tx, WorkerName);
            if (cResult.HasValue)
                return cResult.Value;
            else
                return null;
        }

        protected async Task<bool> ReEnqueAsync(ITransaction tx)
        {
            WorkerOperation.RetryCount++;
            if (WorkerOperation.RetryCount > CtrlSvc.s_MaxWorkerOpeartionRetry)
                return false;

            var operationQ = await StateManager.GetOrAddAsync<IReliableQueue<WorkerOperation>>(CtrlSvc.s_OperationQueueName);
            await operationQ.EnqueueAsync(tx, WorkerOperation);

            return false;
        }



        protected async Task CleanUpServiceFabricCluster(Worker worker)
        {
            try
            {
                await DeleteServiceAsync(worker);
            }
            catch
            {
                // todo: log
            }


            try
            {
                await DeleteAppAsync(worker);
            }
            catch
            {
                //todo: log
            }
        }


        protected async Task DeleteServiceAsync(Worker worker)
        {
            var sServiceName = new Uri(string.Concat(
                                                    worker.ServiceFabricAppName,
                                                    CtrlSvc.DefaultWorkerAppDefinition.ServiceTypeName)
                                                    );

            FabricClient fabricClient = new FabricClient();
            await fabricClient.ServiceManager.DeleteServiceAsync(sServiceName);

        }

        protected async Task DeleteAppAsync(Worker worker)
        {
            FabricClient fabricClient = new FabricClient();
            await fabricClient.ApplicationManager.DeleteApplicationAsync(new Uri(worker.ServiceFabricAppName));
        }


        protected async Task CreateAppAsync(Worker worker)
        {

            FabricClient fabricClient = new FabricClient();
            ApplicationDescription appDesc = new ApplicationDescription(new Uri(worker.ServiceFabricAppName),
                                                                         worker.ServiceFabricAppTypeName,
                                                                         worker.ServiceFabricAppTypeVersion);


            // create the app
            await fabricClient.ApplicationManager.CreateApplicationAsync(appDesc);
        }

        protected async Task CreateServiceAsync(Worker worker)
        {

            var sServiceName = new Uri(string.Concat(
                                                    worker.ServiceFabricAppName,
                                                    "/",
                                                    CtrlSvc.DefaultWorkerAppDefinition.ServiceTypeName)
                                                    );



            FabricClient fabricClient = new FabricClient();
            await fabricClient.ServiceManager.CreateServiceFromTemplateAsync(
                        new Uri(worker.ServiceFabricAppName),
                        sServiceName,
                        CtrlSvc.DefaultWorkerAppDefinition.ServiceTypeName,
                        worker.AsBytes());

        }


        public abstract Task HandleOpeartion( ITransaction tx);
        
    }
}
