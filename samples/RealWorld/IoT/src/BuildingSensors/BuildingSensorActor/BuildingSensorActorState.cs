using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using BuildingSensorActor.Interfaces;
using BuildingSensorActor.Common;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace BuildingSensorActor
{
    [DataContract]
    public class BuildingSensorActorState
    {

        [DataMember]
        public DateTime LatestMessageTime;

        [DataMember]
        public SensorMessage LatestMessageProperties;

    }
}