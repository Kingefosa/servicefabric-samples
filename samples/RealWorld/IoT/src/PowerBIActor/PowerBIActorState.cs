using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using PowerBIActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace PowerBIActor
{
    [DataContract]
    public class PowerBIActorState
    {
        [DataMember]
        public Queue<PowerBIWorkItem> Queue = new Queue<PowerBIWorkItem>();
    }
}