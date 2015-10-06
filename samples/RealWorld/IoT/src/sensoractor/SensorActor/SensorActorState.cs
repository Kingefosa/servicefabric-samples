using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using SensorActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;

namespace SensorActor
{
    [DataContract]
    public class SensorActorState
    {
        [DataMember]
        public DateTime LatestEventTime;

        [DataMember]
        public IDictionary<string, object> LatestEventProperties;

        [DataMember]
        public IList<ISensorDetails> Properties;


        //public override string ToString()
        //{
        //    return string.Format(CultureInfo.InvariantCulture, "SensorActorState[Count = {0}]", Count);
        //}
    }
}