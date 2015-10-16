using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using FloorActor.Interfaces;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;
namespace FloorActor
{
    [DataContract]
    public class FloorActorState
    {
        [DataMember]
        public List<SensorMessage> Messages;
        

        //public override string ToString()
        //{
        //    return string.Format(CultureInfo.InvariantCulture, "FloorActorState[Count = {0}]", Count);
        //}
    }
}