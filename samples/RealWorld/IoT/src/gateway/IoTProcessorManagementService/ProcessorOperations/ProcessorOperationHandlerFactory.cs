using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    public class ProcessorOperationHandlerFactory
    {
        public ProcessorOperationHandlerBase CreateHandler(ProcessorManagementService Svc,
                                             ProcessorOperation Operation)
        {

            switch (Operation.OperationType)
            {
                case ProcessorOperationType.Add:
                   return new ProcessorOperationAddHandler(Svc, Operation);

                case ProcessorOperationType.Delete:
                    return new ProcessorOperationDeleteHandler(Svc, Operation);

                case ProcessorOperationType.Pause:
                    return new ProcessorOperationStatusChangeHandler(Svc, Operation);

                case ProcessorOperationType.Resume:
                    return new ProcessorOperationStatusChangeHandler(Svc, Operation);

                case ProcessorOperationType.Stop:
                    return new ProcessorOperationStatusChangeHandler(Svc, Operation);

                case ProcessorOperationType.DrainStop:
                    return new ProcessorOperationStatusChangeHandler(Svc, Operation);

                case ProcessorOperationType.RuntimeStatusCheck:
                    return new ProcessorOperationStatusChangeHandler(Svc, Operation);

                case ProcessorOperationType.Update:
                    return new ProcessorOperationUpdateHandler(Svc, Operation);


                default:
                   throw new InvalidOperationException("Can not identify Processor Operation");
            }

        }
    }
}
