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
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    class ProcessorOperationDeleteHandler : ProcessorOperationHandlerBase
    {

        public ProcessorOperationDeleteHandler(ProcessorManagementService svc,
                                         ProcessorOperation Opeartion) : base(svc, Opeartion)
        {

        }
        public override async Task RunOperation(ITransaction tx)
        {
            var processor = await GetProcessorAsync(processorOperation.ProcessorName, tx);
            await CleanUpServiceFabricCluster(processor);
            processor.ProcessorStatus &= ~ProcessorStatus.PendingDelete;
            processor.ProcessorStatus |= ProcessorStatus.Deleted;        
            await UpdateProcessorAsync(processor, tx);

            ServiceEventSource.Current.Message(string.Format("Processor:{0} with App:{1} deleted", processor.Name, processor.ServiceFabricServiceName));
        }

        public override Task<T> ExecuteOperation<T>(ITransaction tx)
        {
            throw new NotImplementedException();
        }
    }
}
