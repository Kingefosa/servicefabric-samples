using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

using IoTGateway.Clients;
namespace CtrlSvc
{
    public class WorkerController : ApiController, IReliableStateApiController
    {

         

        // HTTP configuration creates a dependancy resolver that 
        // ensures that state manager is set. 
        public IReliableStateManager StateManager
        {
            get;
            set;
        }


        [HttpGet]
        public async Task<Worker[]> GetAll()
        {
            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
            //dirty read
            return workers.Select((kvp) => { return kvp.Value; }).ToArray();            
        }


        [HttpGet]
        [Route("worker/{WorkerName}")]

        public async Task<Worker> Get(string WorkerName)
        {
            var validationErrors = Worker.ValidateWorkerName(WorkerName);
            Utils.ThrowHttpError(validationErrors);


            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
           
            using (var tx = StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await workers.TryGetValueAsync(tx, WorkerName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Worker with the name {0} does not exist", WorkerName));

                return cResults.Value;
            }

        }


        [HttpPost]
        [Route("worker/{WorkerName}")]
        public async Task<Worker> Add([FromUri] string WorkerName,  [FromBody] Worker worker)
        {
            var validationErrors = Worker.ValidateWorkerName(WorkerName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);

            validationErrors = worker.Validate();
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);


            worker.Name = WorkerName;


            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
            var operationQ = await StateManager.GetOrAddAsync <IReliableQueue<WorkerOperation>>(CtrlSvc.s_OperationQueueName);

            using (var tx = StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await workers.TryGetValueAsync(tx, worker.Name);
                if (cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Worker with the name {0} currently exists with status", worker.Name),
                                        string.Format("worker {0} is currently {1} and mapped to app {2}",
                                                       cResults.Value.Name,
                                                       cResults.Value.WorkerStatus.ToString(),
                                                       cResults.Value.ServiceFabricAppName));

                // save it workers
                await workers.AddAsync(tx, worker.Name, worker);
                // create it it
                await operationQ.EnqueueAsync(tx, new WorkerOperation() { OperationType = WorkerOperationType.Add, WorkerName = worker.Name });

                await tx.CommitAsync();
            }

            return worker;
        }

        [HttpDelete]
        [Route("worker/{WorkerName}")]

        public async Task<Worker> Delete([FromUri] string WorkerName)
        {
            var validationErrors = Worker.ValidateWorkerName(WorkerName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);

            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);
            var operationQ = await StateManager.GetOrAddAsync<IReliableQueue<WorkerOperation>>(CtrlSvc.s_OperationQueueName);


            Worker deleted; 
            using (var tx = StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await workers.TryGetValueAsync(tx, WorkerName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Worker with the name {0} does not exists", WorkerName));

                deleted = cResults.Value;

                
                if(deleted.WorkerStatus == WorkerStatus.PendingDelete || deleted.WorkerStatus== WorkerStatus.Deleted)
                    Utils.ThrowHttpError(string.Format("Worker with the name {0} is deleted or being deleted", WorkerName));

                deleted.WorkerStatus = WorkerStatus.PendingDelete;
                await workers.AddOrUpdateAsync(tx, deleted.Name, deleted, (name, wrk) => { return deleted; });
                // delete it
                await operationQ.EnqueueAsync(tx, new WorkerOperation() { OperationType = WorkerOperationType.Delete, WorkerName = WorkerName });
                await tx.CommitAsync();
            }
            return deleted;
        }



    }
}
