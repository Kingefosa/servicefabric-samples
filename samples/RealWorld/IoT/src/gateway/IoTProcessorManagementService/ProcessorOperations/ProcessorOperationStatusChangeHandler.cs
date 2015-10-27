// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using IoTProcessorManagement.Clients;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    class ProcessorOperationStatusChangeHandler : ProcessorOperationHandlerBase
    {

        public ProcessorOperationStatusChangeHandler(ProcessorManagementService svc,
                                         ProcessorOperation Operation) : base(svc, Operation)
        {

        }


        private async Task Pause(Processor processor)
        {
            
            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Post;
            var tasks = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/pause");
            await Task.WhenAll(tasks);

            ServiceEventSource.Current.Message(string.Format("Processor {0} with App {1} paused", processor.Name, processor.ServiceFabricServiceName));
        }


        private async Task Stop(Processor processor)
        {

            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Post;
            var tasks  = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/stop");
            await Task.WhenAll(tasks);

            ServiceEventSource.Current.Message(string.Format("Processor {0} with App {1} stopped", processor.Name, processor.ServiceFabricServiceName));

        }


        private async Task Resume(Processor processor)
        {
            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Post;
            var tasks = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/resume");
            await Task.WhenAll(tasks);

            ServiceEventSource.Current.Message(string.Format("Processor {0} with App {1} resumed", processor.Name, processor.ServiceFabricServiceName));

        }

        private async Task DrainStop(Processor processor)
        {
            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Post;
             var tasks = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/drainstop");

            // await all tasks if yu want to wait for drain.

            ServiceEventSource.Current.Message(string.Format("Processor {0} with App {1} is going into drain stop phase", processor.Name, processor.ServiceFabricServiceName));

        }

        private async Task<Task<HttpResponseMessage>[]> GetProcessorRuntimeStatusAsync(Processor processor)
        {
            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Get;

            var tasks = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/");
            return tasks;
        }

        public override async Task RunOperation(ITransaction tx)
        {
            var processor = await GetProcessorAsync(processorOperation.ProcessorName, tx);

            if (ProcessorOperationType.Pause == processorOperation.OperationType)
            {
                await Pause(processor);
                processor.ProcessorStatus &= ~ProcessorStatus.PendingPause;
                processor.ProcessorStatus |= ProcessorStatus.Paused;
                await UpdateProcessorAsync(processor, tx);
            }


            if (ProcessorOperationType.Stop == processorOperation.OperationType)
            { 
                await Stop(processor);
                processor.ProcessorStatus &= ~ProcessorStatus.PendingStop;
                processor.ProcessorStatus |= ProcessorStatus.Stopped;
                await UpdateProcessorAsync(processor,tx);
            }
        
            if (ProcessorOperationType.Resume == processorOperation.OperationType)
            { 
                await Resume(processor);
                processor.ProcessorStatus &= ~ProcessorStatus.PendingResume;
                processor.ProcessorStatus &= ~ProcessorStatus.Paused;


                processor.ProcessorStatus |= ProcessorStatus.Provisioned ;
                await UpdateProcessorAsync(processor, tx);
            }


            if (ProcessorOperationType.DrainStop == processorOperation.OperationType)
            { 
                await DrainStop(processor);
                processor.ProcessorStatus &= ~ProcessorStatus.PendingDrainStop;
                processor.ProcessorStatus |= ProcessorStatus.Stopped;
                await UpdateProcessorAsync(processor, tx);
            }

            if (ProcessorOperationType.RuntimeStatusCheck == processorOperation.OperationType)
                throw new InvalidOperationException("Run time status check should not be called using a Task() handler");
                    
         }

        public override async Task<T> ExecuteOperation<T>(ITransaction tx)
        {
            if (processorOperation.OperationType != ProcessorOperationType.RuntimeStatusCheck)
                throw new InvalidOperationException("Execute operation for status change handler can not handle anything except runtime status check");

                var processor = await GetProcessorAsync(processorOperation.ProcessorName, tx);
            return  await GetProcessorRuntimeStatusAsync(processor) as T;
        }
    }
}
