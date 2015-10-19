using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Clients
{
    public class ProcessorRuntimeStatus
    {
        public int TotalPostedLastMinute { get; set; }
        public int TotalProcessedLastMinute { get; set; }
        public int TotalPostedLastHour { get; set; }
        public int TotalProcessedLastHour { get; set; }
        public float AveragePostedPerMinLastHour { get; set; }
        public float AverageProcessedPerMinLastHour { get; set; }
        public string StatusString { get; set; }
        public int NumberOfActiveQueues { get; set; }
        public bool IsInErrorState { get; set; }
        public string ErrorMessage { get; set; }

        public long NumberOfBufferedItems { get; set; }
    }
}
