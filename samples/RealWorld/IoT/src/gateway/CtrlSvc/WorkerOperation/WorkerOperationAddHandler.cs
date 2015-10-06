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
    class WorkerOperationAddHandler : WorkerOpeartionHandler
    {

        public WorkerOperationAddHandler(IReliableStateManager StateManager,
                                         WorkerOperation Opeartion) : base(StateManager, Opeartion)
        {

        }
        public override async Task HandleOpeartion(ITransaction tx)
        {
            var worker = await GetWorkerAsync(WorkerOperation.WorkerName, tx);
            // if there is no default types names assigned, get defaults
            if (string.IsNullOrEmpty(worker.ServiceFabricAppTypeName))
            {
                worker.ServiceFabricAppTypeName = CtrlSvc.DefaultWorkerAppDefinition.AppTypeName;
                worker.ServiceFabricAppTypeVersion = CtrlSvc.DefaultWorkerAppDefinition.AppTypeVersion;
            }

            worker.ServiceFabricAppName = string.Concat("fabric:/", 
                                                        CtrlSvc.DefaultWorkerAppDefinition.WorkerAppNamePrefix, 
                                                        CtrlSvc.DefaultWorkerAppDefinition.AppTypeName,
                                                        DateTime.UtcNow.Ticks
                                                        );

            // save it so we won't lose it
            await SaveWorkerAsync(worker, null, true);
            try
            {
                await CreateAppAsync(worker);
                await CreateServiceAsync(worker);
                worker.WorkerStatus = WorkerStatus.Working;

            }
            catch (FabricElementAlreadyExistsException elementExistEx)
            {
                // no op - we tried to create it before and failed. 

                //todo: log!
            }
            catch (FabricElementNotFoundException elementNotFoundEx)
            {
                worker.WorkerStatus = WorkerStatus.Error;
                worker.ErrorMessage = string.Format("Failed to create service fabric app {0} [{1}-{2}] Error:{3}",
                                                    worker.ServiceFabricAppName,
                                                    worker.ServiceFabricAppTypeName,
                                                    worker.ServiceFabricAppTypeVersion,
                                                    elementNotFoundEx.Message);
            }
            catch (Exception e)
            {
                // did we try enough?
                if (!await ReEnqueAsync(tx))
                {
                    worker.WorkerStatus = WorkerStatus.Error;
                    worker.ErrorMessage = string.Format("Failed to create service fabric app {0} [{1}-{2}] Error:{3} \n after{4} times",
                                                        worker.ServiceFabricAppName,
                                                        worker.ServiceFabricAppTypeName,
                                                        worker.ServiceFabricAppTypeVersion,
                                                        e.Message,
                                                        WorkerOperation.RetryCount -1);

                        await CleanUpServiceFabricCluster(worker);   
                }
            }
            finally
            {
                await SaveWorkerAsync(worker, tx);
            }    
        }

        

    }
}
