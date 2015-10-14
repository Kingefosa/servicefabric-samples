using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using DataArchiveActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;

namespace DataArchiveActor
{
    [DataContract]
    public class DataArchiveActorState
    {
        [DataMember]
        public List<SensorMessage> SensorMessages;
    }
}