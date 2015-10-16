using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorActor.Common
{
    public class SensorMessage
    {
        public string DeviceId { get; set; }
        public string FloorId { get; set; }
       public double TempF { get; set; }
        public double Humidity { get; set; }
        public string Motion { get; set; }
        public bool Light { get; set; }
        public DateTime Time { get; set; }
    }
}
