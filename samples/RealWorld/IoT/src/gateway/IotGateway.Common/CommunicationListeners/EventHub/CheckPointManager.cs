using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    class CheckPointManager : ICheckpointManager
    {
        public async Task CheckpointAsync(Lease lease, string offset, long sequenceNumber)
        {
            var stateManagerLease = lease as StateManagerLease;

            stateManagerLease.Offset = offset;
            stateManagerLease.SequenceNumber = sequenceNumber;
            await stateManagerLease.SaveAsync();
        }
    }
}
