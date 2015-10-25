using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using IoTActor.Common;

namespace StorageActor
{
    [DataContract]
    public class StorageActorState
    {
        [DataMember]
        public Queue<IoTActorWorkItem> Queue = new Queue<IoTActorWorkItem>();
    }
}