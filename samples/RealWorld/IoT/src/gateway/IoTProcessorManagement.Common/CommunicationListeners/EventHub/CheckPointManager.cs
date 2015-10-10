using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// vanila implementation of ICheckpointManager (refer to Event Hub SDK).
    /// the check point manager sets the lease and presist it in Service Fabric state.
    /// </summary>
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
