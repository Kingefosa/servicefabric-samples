using IoTProcessorManagement.Clients;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    public class ProcessorOperationAddHandler : ProcessorOperationHandlerBase
    {

        public ProcessorOperationAddHandler(ProcessorManagementService svc,
                                         ProcessorOperation Opeartion) : base(svc, Opeartion)
        {

        }
        public override async Task RunOperation(ITransaction tx)
        {
            var processor = await GetProcessorAsync(processorOperation.ProcessorName);
            // if there is no default types names assigned, get defaults
            if (string.IsNullOrEmpty(processor.ServiceFabricAppTypeName))
            {
                processor.ServiceFabricAppTypeName = Svc.DefaultProcessorAppInstanceDefinition.AppTypeName;
                processor.ServiceFabricAppTypeVersion = Svc.DefaultProcessorAppInstanceDefinition.AppTypeVersion;
            
                processor.ServiceFabricAppInstanceName = string.Concat("fabric:/",
                                                            Svc.DefaultProcessorAppInstanceDefinition.AppInstanceNamePrefix,
                                                            Svc.DefaultProcessorAppInstanceDefinition.AppTypeName,
                                                            DateTime.UtcNow.Ticks
                                                            );

                processor.ServiceFabricServiceName = string.Concat(
                                                        processor.ServiceFabricAppInstanceName,
                                                        "/",
                                                        Svc.DefaultProcessorAppInstanceDefinition.ServiceTypeName);

            }

            // save it so we won't lose it
            await UpdateProcessorAsync(processor, null, true, true);
            try
            {
                await CreateAppAsync(processor);
                await CreateServiceAsync(processor);
                processor.ProcessorStatus &= ~ProcessorStatus.New;
                processor.ProcessorStatus = ProcessorStatus.Provisioned ;

            }
            catch (FabricElementAlreadyExistsException elementExistEx)
            {
                var sMessage = string.Format("An Add processor Operation has failed, application was partially created before {0}. overall the Add Processor operation was sucessful ", elementExistEx.Message) ;
                ServiceEventSource.Current.Message(sMessage);


            }
            catch (FabricElementNotFoundException elementNotFoundEx)
            {
                processor.ProcessorStatus |= ProcessorStatus.ProvisionError;
                processor.ErrorMessage = string.Format("Failed to create service fabric app {0} [{1}-{2}] Error:{3}",
                                                    processor.ServiceFabricAppInstanceName,
                                                    processor.ServiceFabricAppTypeName,
                                                    processor.ServiceFabricAppTypeVersion,
                                                    elementNotFoundEx.Message);

                ServiceEventSource.Current.Message("Processor creation {0} failed - {1}", processor.Name, processor.ErrorMessage);
            }
            catch (Exception e)
            {
                // did we try enough?
                if (!await ReEnqueAsync(tx))
                {
                    processor.ProcessorStatus |= ProcessorStatus.ProvisionError;
                    processor.ErrorMessage = string.Format("Failed to create service fabric app {0} [{1}-{2}] Error:{3} \n after{4} times",
                                                        processor.ServiceFabricAppInstanceName,
                                                        processor.ServiceFabricAppTypeName,
                                                        processor.ServiceFabricAppTypeVersion,
                                                        e.Message,
                                                        processorOperation.RetryCount -1);

                    ServiceEventSource.Current.Message("Processor creation {0} failed - {1}", processor.Name, processor.ErrorMessage);

                    await CleanUpServiceFabricCluster(processor);   
                }
            }
            finally
            {
                await UpdateProcessorAsync(processor, null, true, true);
            }


            ServiceEventSource.Current.Message("Processor creation {0} done", processor.Name);

        }

        public override Task<T> ExecuteOperation<T>(ITransaction tx)
        {

            // add operation does not support return values. 
            throw new NotImplementedException();
        }
    }
}
