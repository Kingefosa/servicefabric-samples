using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

using IoTProcessorManagement.Clients;
using Newtonsoft.Json;
using System.Net.Http;

namespace IoTProcessorManagementService
{
    public class ProcessorController : ApiController, ProcessorManagementServiceApiController
    {

        /// <summary>
        ///   HTTP configuration creates a dependancy resolver that ensures that service ref is set .
        /// </summary>
        public ProcessorManagementService Svc
        {
            get;
            set;
        }


        [HttpGet]
        [Route("processor/")]
        public  Task<Processor[]> GetAll()
        {
            //dirty read
            return Task.FromResult(Svc.ProcessorStateStore.Select((kvp) => { return kvp.Value; }).ToArray());            
        }


        [HttpGet]
        [Route("processor/{ProcessorName}/detailed")]
        public async Task<List<ProcessorRuntimeStatus>> GetDetailedStatus([FromUri] string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);

            Processor processor;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exist", ProcessorName));

                processor = cResults.Value;
            }

            var factory = new ProcessorOperationHandlerFactory();
            var operationHandler = factory.CreateHandler(this.Svc, new ProcessorOperation() { OperationType = ProcessorOperationType.RuntimeStatusCheck, ProcessorName = ProcessorName });

            List<ProcessorRuntimeStatus> runtimeStatus = new List<ProcessorRuntimeStatus>();
            Task<HttpResponseMessage>[] tasks = await operationHandler.ExecuteOperation<Task<HttpResponseMessage>[]>(null);

            await Task.WhenAll(tasks);

            foreach (var completedTask in tasks)
            {
                var httpResponse = completedTask.Result;
                if (!httpResponse.IsSuccessStatusCode)
                    Utils.ThrowHttpError("error aggregating status from processor partitions");

                runtimeStatus.Add(JsonConvert.DeserializeObject<ProcessorRuntimeStatus>(await httpResponse.Content.ReadAsStringAsync()));
            }
            return runtimeStatus;
        }

        [HttpGet]
        [Route("processor/{ProcessorName}")]
        public async Task<Processor> Get([FromUri]  string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);

            
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exist", ProcessorName));

                return cResults.Value;
            }

        }



       

        [HttpPost]
        [Route("processor/{ProcessorName}")]
        public async Task<Processor> Add([FromUri] string ProcessorName,  [FromBody] Processor processor)
        {
            processor.Name = ProcessorName;

            var validationErrors = processor.Validate();
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);


            processor.ProcessorStatus = ProcessorStatus.New;

            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, processor.Name);
                if (cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} currently exists with status", processor.Name),
                                        string.Format("Processor {0} is currently {1} and mapped to app {2}",
                                                       cResults.Value.Name,
                                                       cResults.Value.ProcessorStatus.ToString(),
                                                       cResults.Value.ServiceFabricAppInstanceName));

                // save it 
                await Svc.ProcessorStateStore.AddAsync(tx, processor.Name, processor);
                // create it it
                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Add, ProcessorName = processor.Name });
                

                await tx.CommitAsync();

                ServiceEventSource.Current.Message(string.Format("Queued create for processor {0} ", processor.Name));

            }

            
            return processor;
        }

        [HttpDelete]
        [Route("processor/{ProcessorName}")]
        public async Task<Processor> Delete([FromUri] string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);



            Processor existing; 
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;

                
                if(existing.IsOkToDelete())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.ProcessorStatus |= ProcessorStatus.PendingDelete;
                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing); return proc; });
                // delete it
                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Delete, ProcessorName = ProcessorName });
                await tx.CommitAsync();
            }
            return existing;
        }




        #region Per Processor  Actions

        [HttpPut]
        [Route("processor/{ProcessorName}")]
        public async Task<Processor> Update([FromUri] string ProcessorName, [FromBody] Processor processor)
        {
            processor.Name = ProcessorName;

            var validationErrors = processor.Validate();
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);


            Processor existing;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;


                if (existing.IsOkToQueueOperation())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.Hubs = processor.Hubs;
                existing.ProcessorStatus |= ProcessorStatus.PendingUpdate;
                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing, false, true); return proc; });

                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Update, ProcessorName = ProcessorName });
                await tx.CommitAsync();
                ServiceEventSource.Current.Message(string.Format("Queued pause command for Processor {0} ", existing.Name));
            }

            return existing;

        }


        [HttpPost]
        [Route("processor/{ProcessorName}/pause")]
        public async Task<Processor> Pause([FromUri] string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);





            Processor existing;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;


                if (existing.IsOkToQueueOperation())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.ProcessorStatus |= ProcessorStatus.PendingPause;
                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing); return proc; });
                
                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Pause, ProcessorName = ProcessorName });
                await tx.CommitAsync();
                ServiceEventSource.Current.Message(string.Format("Queued pause command for Processor {0} ", existing.Name));

            }

            return existing;
        }

        [HttpPost]
        [Route("processor/{ProcessorName}/stop")]
        public async Task<Processor> Stop([FromUri] string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);





            Processor existing;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;


                if (existing.IsOkToQueueOperation())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.ProcessorStatus |= ProcessorStatus.PendingStop;

                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing); return proc; });


                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Stop, ProcessorName = ProcessorName });
                await tx.CommitAsync();


                ServiceEventSource.Current.Message(string.Format("Queued stop command for processor {0} ", existing.Name));

            }

            return existing;

        }

        [HttpPost]
        [Route("processor/{ProcessorName}/resume")]
        public async Task<Processor> Resume([FromUri] string ProcessorName)
        {
            var validationErrors = IoTProcessorManagement.Clients.Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);



            Processor existing;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;


                if (existing.IsOkToQueueOperation())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.ProcessorStatus |= ProcessorStatus.PendingResume;
                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing); return proc; });


                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.Resume, ProcessorName = ProcessorName });
                await tx.CommitAsync();

                ServiceEventSource.Current.Message(string.Format("Queued resume command for processor {0} ", existing.Name));

            }

            return existing;
        }


        [HttpPost]
        [Route("processor/{ProcessorName}/drainstop")]
        public async Task<Processor> DrainStop([FromUri] string ProcessorName)
        {
            var validationErrors = Processor.ValidateProcessName(ProcessorName);
            if (null != validationErrors)
                Utils.ThrowHttpError(validationErrors);


            Processor existing;
            using (var tx = Svc.StateManager.CreateTransaction())
            {
                // do we have it? 
                var cResults = await Svc.ProcessorStateStore.TryGetValueAsync(tx, ProcessorName);
                if (!cResults.HasValue)
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} does not exists", ProcessorName));

                existing = cResults.Value;


                if (existing.IsOkToQueueOperation())
                    Utils.ThrowHttpError(string.Format("Processor with the name {0} not valid for this operation", ProcessorName, existing.ProcessorStatusString));

                existing.ProcessorStatus |= ProcessorStatus.PendingDrainStop;
                existing = await Svc.ProcessorStateStore.AddOrUpdateAsync(tx, existing.Name, existing, (name, proc) => { proc.SafeUpdate(existing); return proc; });
                await Svc.ProcessorOperationsQueue.EnqueueAsync(tx, new ProcessorOperation() { OperationType = ProcessorOperationType.DrainStop, ProcessorName = ProcessorName });
                await tx.CommitAsync();

                ServiceEventSource.Current.Message(string.Format("Queued drain/stop command for processor {0} ", existing.Name));

            }


            return existing;
        }


        #endregion






    }
}
