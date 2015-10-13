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
           var proxy = ActorProxy.Create<IBuildingSensorActor>(ActorId.NewId(), "fabric:/BuildingSensors");

            Console.WriteLine("Time:" + proxy.GetLastMessageTime().Result);
            Console.WriteLine("Device Id:" + proxy.GetDeviceId().Result);
            Console.WriteLine("Building Id:" + proxy.GetBuildingId().Result);
            Console.WriteLine("Humity:" + proxy.GetHumityPercentage().Result);
            Console.WriteLine("Light Status:" + proxy.GetLightStatus().Result);
            Console.WriteLine("Temp:" + proxy.GetTemperatureInFahrenheit().Result);

            SensorMessage m = new SensorMessage() { DeviceId = "DeviceId", BuildingId = "Building1234", Humidity = 32, Light = true, Motion = "false", TempF = 72 };
            string jsonString = JsonConvert.SerializeObject(m);
            proxy.ReceiveDeviceState(DateTime.Now, Encoding.UTF8.GetBytes(jsonString));
            Console.WriteLine("---");
            Console.WriteLine("Time:" + proxy.GetLastMessageTime().Result);
            Console.WriteLine("Device Id:" + proxy.GetDeviceId().Result);
            Console.WriteLine("Building Id:" + proxy.GetBuildingId().Result);
            Console.WriteLine("Humity:" + proxy.GetHumityPercentage().Result);
            Console.WriteLine("Light Status:" + proxy.GetLightStatus().Result);
            Console.WriteLine("Temp:" + proxy.GetTemperatureInFahrenheit().Result);
        }
    }
}
