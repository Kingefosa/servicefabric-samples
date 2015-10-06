using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorActor.Interfaces
{
    public class ISensorDetails
    {
        public DateTime Time { get; set; }
        public string PropertyName { get; set; }
        public object Value { get; set; }
    }
}
