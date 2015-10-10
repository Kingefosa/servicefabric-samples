using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    public class ProcessorOperation
    {
        public string ProcessorName { get; set; }
        public ProcessorOperationType OperationType { get; set; }

        public int RetryCount { get; set; }

        public ProcessorOperation()
        {
            RetryCount = 1;
        }

    }
    public enum ProcessorOperationType
    {
        Add, 
        Pause, 
        Resume,
        Stop, 
        DrainStop, 
        Delete,
        RuntimeStatusCheck
    }
}
