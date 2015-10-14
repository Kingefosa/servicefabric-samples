using SensorActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using SensorActor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DataArchiveActor.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Sensors.TestClient
{
    public class Program
    {
        private const string ServiceUri = "fabric:/IoTApplication";
        public static void Main(string[] args)
        {
            //TestSensorActor().Wait();
            TestDataArchiveActor().Wait();
        }

        private static async Task TestSensorActor()
        {
            var proxy = ActorProxy.Create<ISensorActor>(ActorId.NewId(), ServiceUri);

            DateTime t = await proxy.GetLastMessageTimeAsync();

            Console.WriteLine("Time:" + await proxy.GetLastMessageTimeAsync());
            Console.WriteLine("Device Id:" + await proxy.GetDeviceIdAsync());
            Console.WriteLine("Building Id:" + await proxy.GetBuildingIdAsync());
            Console.WriteLine("Humity:" + await proxy.GetHumityPercentageAsync());
            Console.WriteLine("Light Status:" + await proxy.GetLightStatusAsync());
            Console.WriteLine("Temp:" + await proxy.GetTemperatureInFahrenheitAsync());

            SensorMessage m = new SensorMessage() { DeviceId = "DeviceId", FloorId = "Building1234", Humidity = 32, Light = true, Motion = "false", TempF = 72 };
            string jsonString = JsonConvert.SerializeObject(m);
            await proxy.SendDeviceStateAsync(DateTime.Now, Encoding.UTF8.GetBytes(jsonString));

            Console.WriteLine("---");
            Console.WriteLine("Time:" + await proxy.GetLastMessageTimeAsync());
            Console.WriteLine("Device Id:" + await proxy.GetDeviceIdAsync());
            Console.WriteLine("Building Id:" + await proxy.GetBuildingIdAsync());
            Console.WriteLine("Humity:" + await proxy.GetHumityPercentageAsync());
            Console.WriteLine("Light Status:" + await proxy.GetLightStatusAsync());
            Console.WriteLine("Temp:" + await proxy.GetTemperatureInFahrenheitAsync());
        }
        private static async Task TestDataArchiveActor()
        {
            var proxy = ActorProxy.Create<IDataArchiveActor>(new ActorId("1-1"), ServiceUri);
            await proxy.SaveDeviceStateAsync(new SensorMessage {
                DeviceId = "1",
                FloorId = "1",
                Humidity = 123.456,
                Light = true,
                Motion = "In Motion",
                TempF = -45.67
            });
        }
    }
    class SensorDataEntity : TableEntity
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
