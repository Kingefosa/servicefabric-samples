using IoTGateway.Clients;
using IoTGateway.TestLib;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTGateway.TestHarness
{
    class Program
    {
        private static readonly string s_FabricEndPoint = "localhost:19000"; // update id you are using a remote cluster. 
        private static readonly string s_GwAppInstanceName = "fabric:/IoTGateway";
        private static readonly string s_GwServicename = string.Concat(s_GwAppInstanceName, "/", "CtrlSvc");


        static void Main(string[] args)
        {

            TestLib.IoTGatewayTestLib.sendMessages("",
                                                    "eh01",
                                                    "device{0}",
                                                    "Hello World!",
                                                    100,
                                                    5).Wait();
            Console.Write("Done!");
            Console.Read();
            return;

            var sBaseAddress = getGwEndPoint().Result;

            var Worker1 = new Worker()
            {
                Name = "One"
            };
            Worker1.Hubs.Add(  new EventHubDefinition()
            {
                ConnectionString = "",
                EventHubName = "",
                ConsumerGroupName =""
            });



            var Worker2 = new Worker()
            {
                Name = "Two"
            };
            Worker2.Hubs.Add(new EventHubDefinition()
            {
                ConnectionString = "",
                EventHubName = "",
                ConsumerGroupName = ""
            });



            // Add
            doAdd(sBaseAddress, Worker1).Wait();
            doAdd(sBaseAddress, Worker2).Wait();

            for (var i = 0; i <= 4; i++)
                doGetAll(sBaseAddress).Wait();

            Console.WriteLine("sleeping 5 secs");
            Console.WriteLine("");

            Thread.Sleep(5 * 1000);

            doDelete(sBaseAddress, "One").Wait();
            doDelete(sBaseAddress, "Two").Wait();

            for (var i = 0; i <= 4; i++)
                doGetAll(sBaseAddress).Wait();


            Console.Write("Done!");

            Console.Read();

        }

        public static async Task<string> getGwEndPoint()
        {
            FabricClient fc = new FabricClient(s_FabricEndPoint);
            var partition = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(s_GwServicename));

            return partition.GetEndpoint().Address;
        }

        public static async Task doDelete(string baseAddress, string WorkerName)
        {
            var response = await IoTGatewayTestLib.doDelete(baseAddress, WorkerName);
            await formatResponse("Delete", response);
        }
        public static async Task doAdd(string baseAddress, Worker worker)
        {
            

            var response = await IoTGatewayTestLib.doAdd(baseAddress, worker);
            await formatResponse("Add", response);

        }


        public static async Task doGetOne(string baseAddress, string WorkerName)
        {
            var response = await IoTGatewayTestLib.doGetOne(baseAddress, WorkerName);
            await formatResponse("GetOne", response);
        }

        public static async Task doGetAll(string baseAddress)
        {
            var response = await IoTGatewayTestLib.doGetAll(baseAddress);
            await formatResponse("Getall", response);
        }

        private static async Task formatResponse(string OpeartionName, HttpResponseMessage response)
        {
            Console.WriteLine(string.Format("{0} was:{1}", OpeartionName, response.IsSuccessStatusCode ? "Ok" : "fail"));
            Console.WriteLine(string.Format("{0} response was:{1}", OpeartionName, await response.Content.ReadAsStringAsync()));
            Console.WriteLine("");

        }

    }
}
