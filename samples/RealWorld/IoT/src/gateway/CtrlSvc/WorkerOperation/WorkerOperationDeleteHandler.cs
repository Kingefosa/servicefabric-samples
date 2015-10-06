using IoTGateway.Clients;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrlSvc
{
    class WorkerOperationDeleteHandler : WorkerOpeartionHandler
    {

        public WorkerOperationDeleteHandler(IReliableStateManager StateManager,
                                         WorkerOperation Opeartion) : base(StateManager, Opeartion)
        {

        }
        public override async Task HandleOpeartion(ITransaction tx)
        {
            var worker = await GetWorkerAsync(WorkerOperation.WorkerName, tx);
            await CleanUpServiceFabricCluster(worker);
            worker.WorkerStatus = WorkerStatus.Deleted;        
            await SaveWorkerAsync(worker, tx);        
        }
    }
}
