using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using SensorActor.Interfaces;
using SensorActor.Common;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace SensorActor
{
    [DataContract]
    public class SensorActorState
    {

        [DataMember]
        public DateTime LatestMessageTime;

        [DataMember]
        public SensorMessage LatestMessageProperties;

    }
}