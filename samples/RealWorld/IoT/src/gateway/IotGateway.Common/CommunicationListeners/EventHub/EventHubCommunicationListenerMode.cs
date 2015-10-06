using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public enum EventHubCommunicationListenerMode
    {
        FairDistribute,
        Distribute,
        OneToOne,
        Single
    }
}
