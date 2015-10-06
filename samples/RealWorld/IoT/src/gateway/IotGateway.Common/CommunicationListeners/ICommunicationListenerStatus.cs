using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public enum ICommunicationListenerStatus
    {
        Closed,
        Opening,
        Opened,  
        Closing,
        Initializing,
        Initialized,
        Aborting,
        Aborted
    }
}
