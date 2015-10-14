using Microsoft.WindowsAzure.Storage.Table;
using SensorActor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataArchiveActor
{
    class SensorDataEntity: TableEntity
    {
        public string DeviceId { get; set; }
        public string FloorId { get; set; }
        public double Humidity { get; set; }
        public bool Light { get; set; }
        public string Motion { get; set; }
        public double TempF { get; set; }
        public SensorDataEntity(SensorMessage message)
        {
            this.PartitionKey = message.FloorId + "-" + message.DeviceId;
            this.RowKey = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            this.DeviceId = message.DeviceId;
            this.FloorId = message.FloorId;
            this.Humidity = message.Humidity;
            this.Light = message.Light;
            this.Motion = message.Motion;
            this.TempF = message.TempF;
        }
    }
}
