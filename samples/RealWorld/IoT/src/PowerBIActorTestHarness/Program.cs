using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using PowerBIActor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIActorTestHarness
{
    class Program
    {
        
        private static Uri serviceUri = new Uri("fabric:/IoTApplication/PowerBIActorService");
        private static ActorId actorId = ActorId.NewId();
        private static IPowerBIActor powerBiActor = ActorProxy.Create<IPowerBIActor>(actorId, serviceUri);
        
        static void Main(string[] args)
        {
            doPowerBiTest(1000).Wait();

            Console.WriteLine("Done!");
            Console.Read();

              
        }

        private static async Task doPowerBiTest(int numofCalls)
        {
            var rand = new Random();
            for (var i = 1; i <= numofCalls; i++)
            {
                var Event = new
                {
                    DeviceId = string.Concat("d" + rand.Next(1, 10)),
                    BuildingId = "b1",
                    TempF = rand.Next(1,100).ToString(),
                    Humidity = rand.Next(1, 100).ToString(),
                    Motion = rand.Next(1,10).ToString(),
                    light = rand.Next(1,10).ToString(),
                    EventDate = DateTime.UtcNow.ToString()
                };
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Event));

                await powerBiActor.Post("d1", "b1", "eh01", bytes);
            }
        }
    }
}
