using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public enum WorkManagerStatus
    {
        New, 
        Working,
        Paused,
        Stopped,
        Draining
    }
}
