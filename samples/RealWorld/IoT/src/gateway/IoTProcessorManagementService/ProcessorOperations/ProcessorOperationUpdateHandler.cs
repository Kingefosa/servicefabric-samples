using IoTProcessorManagement.Clients;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;
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
    class ProcessorOperationUpdateHandler : ProcessorOperationHandlerBase
    {

        public ProcessorOperationUpdateHandler(ProcessorManagementService svc,
                                         ProcessorOperation Operation) : base(svc, Operation)
        {

        }

        private async Task SendUpdateMessages(Processor processor)
        {

            var requestMessage = await GetBasicPartitionHttpRequestMessageAsync();
            requestMessage.Method = HttpMethod.Put;
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            var tasks = await SendHttpAllServicePartitionAsync(processor.ServiceFabricServiceName, requestMessage, "eventhubprocessor/");
            await Task.WhenAll(tasks);

            ServiceEventSource.Current.Message(string.Format("Processor {0} with App {1} updated", processor.Name, processor.ServiceFabricServiceName));
        }

        public override async Task RunOperation(ITransaction tx)
        {
            var processor = await GetProcessorAsync(processorOperation.ProcessorName, tx);
            await SendUpdateMessages(processor);

                processor.ProcessorStatus &= ~ProcessorStatus.PendingUpdate;
                processor.ProcessorStatus |= ProcessorStatus.Updated;
                await UpdateProcessorAsync(processor, tx);
        }

        public override  Task<T> ExecuteOperation<T>(ITransaction tx)
        {
            // Update operation does not support return values. 
            throw new NotImplementedException();
        }
    }
}
