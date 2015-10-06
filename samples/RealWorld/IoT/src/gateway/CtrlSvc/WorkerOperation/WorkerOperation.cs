using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtrlSvc
{
    public class WorkerOperation
    {
        public string WorkerName { get; set; }
        public WorkerOperationType OperationType { get; set; }

        public int RetryCount { get; set; }

        public WorkerOperation()
        {
            RetryCount = 1;
        }

    }
    public enum WorkerOperationType
    {
        Add, 
        Pause, 
        Start, 
        Update, 
        Delete
    }
}
