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

namespace PowerBIActor
{
    [DataContract]
    public class PowerBIActorState
    {
        [DataMember]
        public Queue<IoTActorWorkItem> Queue = new Queue<IoTActorWorkItem>();
    }
}