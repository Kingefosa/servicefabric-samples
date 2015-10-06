using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrlSvc
{
    class WorkerOpeartionHandlerFactory
    {
        public static WorkerOpeartionHandler Create(IReliableStateManager StateManager,
                                                   WorkerOperation Operation)
        {

            switch (Operation.OperationType)
            {
                case WorkerOperationType.Add:
                   return new WorkerOperationAddHandler(StateManager, Operation);
                case WorkerOperationType.Delete:
                    return new WorkerOperationDeleteHandler(StateManager, Operation);
                                        
                default:
                   throw new InvalidOperationException("Can not identify worker operation");
            }

        }
    }
}
