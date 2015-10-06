using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Clients
{
    public enum WorkerStatus
    {
        New,
        Working,
        PendingDelete,
        Paused,
        Error,
        Deleted
    }
}
