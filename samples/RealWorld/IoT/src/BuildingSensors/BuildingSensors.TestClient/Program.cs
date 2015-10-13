using BuildingSensorActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using BuildingSensorActor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BuildingSensors.TestClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestActor().Wait();
        }

        private static async Task TestActor()
        {
       var proxy = ActorProxy.Create<IBuildingSensorActor>(ActorId.NewId(), "fabric:/BuildingSensors");

            DateTime t = await proxy.GetLastMessageTimeAsync();

            Console.WriteLine("Time:" + await proxy.GetLastMessageTimeAsync());
            Console.WriteLine("Device Id:" +  await proxy.GetDeviceIdAsync());
            Console.WriteLine("Building Id:" + await proxy.GetBuildingIdAsync());
            Console.WriteLine("Humity:" + await proxy.GetHumityPercentageAsync());
            Console.WriteLine("Light Status:" + await proxy.GetLightStatusAsync());
            Console.WriteLine("Temp:" + await proxy.GetTemperatureInFahrenheitAsync());

            SensorMessage m = new SensorMessage() { DeviceId = "DeviceId", BuildingId = "Building1234", Humidity = 32, Light = true, Motion = "false", TempF = 72 };
            string jsonString = JsonConvert.SerializeObject(m);
            await proxy.ReceiveDeviceStateAsync(DateTime.Now, Encoding.UTF8.GetBytes(jsonString));

            Console.WriteLine("---");
            Console.WriteLine("Time:" + await proxy.GetLastMessageTimeAsync());
            Console.WriteLine("Device Id:" + await proxy.GetDeviceIdAsync());
            Console.WriteLine("Building Id:" + await proxy.GetBuildingIdAsync());
            Console.WriteLine("Humity:" + await proxy.GetHumityPercentageAsync());
            Console.WriteLine("Light Status:" + await proxy.GetLightStatusAsync());
            Console.WriteLine("Temp:" + await proxy.GetTemperatureInFahrenheitAsync());


        }
    }
}
