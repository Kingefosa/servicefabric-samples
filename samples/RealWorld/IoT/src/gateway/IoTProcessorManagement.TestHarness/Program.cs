using IoTProcessorManagement.Clients;
using IoTProcessorManagement.TestLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTProcessorManagement.TestHarness
{
    class Program
    {
        private static readonly string s_FabricEndPoint = "localhost:19000"; // update this you are using a remote cluster. 
        private static readonly string s_MgmtStaticAppInstanceName = "fabric:/IoTProcessorManagementApp";
        private static readonly string s_ProcessorStaticAppInstanceName = "fabric:/IoTEventHubProcessorApp";

        private static readonly string s_MgmtServicename = string.Concat(s_MgmtStaticAppInstanceName, "/", "ProcessorManagementService");

        // for tests targeting processor end point
        private static readonly string s_ProcessorServicename = string.Concat(s_ProcessorStaticAppInstanceName, "/", "EventHubProcessorService");

        // set by SetupProcessor
        private static List<Processor> StaticProcessorsDefs  = new List<Processor>();
        // added to the management
        private static List<Processor> RuntimeProcessorDefs = new List<Processor>();


        static void Main(string[] args)
        {
            SetWorkerDefs();
            doMgmtAppTests().Wait();


            Console.Write("Done!");
            Console.Read();

        }

        private static void SetWorkerDefs()
        {
            // add 1 worker defs 

            var processor = new Processor();
            processor.Name = "W1";
            processor.Hubs.Add(new EventHubDefinition() { ConnectionString = "//event hub connection string here//", EventHubName = "eh01" });

            StaticProcessorsDefs.Add(processor);

        }

        public static async Task doSendMessages()
        {
            await IoTManagementTestLib.sendMessages("//eventhub connection string here//",
                                                   "eh01",
                                                   "device{0}",
                                                   "Hello World!",
                                                   100,
                                                   5);
        }


        private static async Task formatResponse(string ActionName, HttpResponseMessage response)
        {
            Console.WriteLine(string.Format("{0} was:{1}", ActionName, response.IsSuccessStatusCode ? "Ok" : "fail"));
            Console.WriteLine(string.Format("{0} response was:{1}", ActionName, await response.Content.ReadAsStringAsync()));
            Console.WriteLine("");

        }



        #region Processor Tests     
            /*
            the below tests are targeting a single process (with a singlton partition)
            used only for development/testing validation
            */

        public static async Task doProcessorTests()
        {
            // EndPoint
            var sProcessorEndPoint = await getGwProcessorEndPoint(s_ProcessorStaticAppInstanceName);


            // Pause 
            await PauseProcessor(sProcessorEndPoint);

            Console.WriteLine("processor paused..");
            
            // Get statuss
            await GetProcessorStatus(sProcessorEndPoint);


            // Resume
            await ResumeProcessor(sProcessorEndPoint);
            Console.WriteLine("processor resumed ..");


            // Get status
            await GetProcessorStatus(sProcessorEndPoint);

            var t = DrainStopProcessor(sProcessorEndPoint);
            Console.WriteLine("processor drainstoped..");

            // loop printint status
            for (var i = 0; i < 10; i++)
            { 
                // Get WorkerStatus
                await GetProcessorStatus(sProcessorEndPoint);
                await Task.Delay(3000);
            }

            await t;
        }
        public static async Task DrainStopProcessor(string baseAddress)
        {
            var response = await IoTProcessorTestLib.doDrainStop(baseAddress);
            await formatResponse("Drain Stop Processor", response);
        }

        public static async Task PauseProcessor(string baseAddress)
        {
            var response =  await IoTProcessorTestLib.doPause(baseAddress);
            await formatResponse("Pause Processor", response);
        }

        public static async Task ResumeProcessor(string baseAddress)
        {
            var response = await IoTProcessorTestLib.doResume(baseAddress);
            await formatResponse("Resume Processor", response);
        }

        public static async Task GetProcessorStatus(string baseAddress)
        {
            var response = await IoTProcessorTestLib.doGetProcessorStatus(baseAddress);
            await formatResponse("Get Processor Status", response);
        }
        public static async Task<string> getGwProcessorEndPoint(string sWorkerAppInstanceName)
        {
            FabricClient fc = new FabricClient(s_FabricEndPoint);
            var partition = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(sWorkerAppInstanceName));

            return partition.GetEndpoint().Address;
        }



        #endregion;

        #region Managment Tests

        /*
        These tests are against the management app end point.
        */

        public static async Task doMgmtAppTests()
        {
            var MgmtAppEndPoint = await getMgmtEndPoint(s_MgmtServicename);

            // add all processor defs 
            foreach (var processor in StaticProcessorsDefs)
                RuntimeProcessorDefs.Add( await AddProcessor(MgmtAppEndPoint, processor));


            Console.WriteLine("Processor Service Fabric App Assignment:");

            foreach (var processor in RuntimeProcessorDefs)
                Console.WriteLine(JsonConvert.SerializeObject(processor));



            Console.WriteLine("Pausing Processors");



            // pause all 
            foreach (var processor in RuntimeProcessorDefs)
                await PauseProcessor(MgmtAppEndPoint, processor.Name);



            for (var i = 1; i < 5; i++)
            {
                await Task.Delay(2000);

                foreach (var processor in RuntimeProcessorDefs)
                    await GetProcessor(MgmtAppEndPoint, processor.Name);

            }


            // resume all 
            foreach (var processor in RuntimeProcessorDefs)
                await ResumeProcessor(MgmtAppEndPoint, processor.Name);


            for (var i = 1; i < 5; i++)
            {
                await Task.Delay(3000);

                foreach (var processor in RuntimeProcessorDefs)
                    await GetProcessor(MgmtAppEndPoint, processor.Name);

            }


            // DrainStop all 
            foreach (var processor in RuntimeProcessorDefs)
                await DrainStopProcessor(MgmtAppEndPoint, processor.Name);


            for (var i = 1; i < 5; i++)
            {
                await Task.Delay(3000);

                foreach (var processor in RuntimeProcessorDefs)
                    await GetProcessor(MgmtAppEndPoint, processor.Name);

            }


            // get all processors
            Console.WriteLine("Processor List");
            await GetAllProcessors(MgmtAppEndPoint);


            Console.WriteLine("will wait 2 sec then delete them");
            await Task.Delay(2000);

            // delete 
            foreach (var processor in RuntimeProcessorDefs)
                await DeleteProcessor(MgmtAppEndPoint, processor.Name);


            Console.WriteLine("Processor List");
            await GetAllProcessors(MgmtAppEndPoint);


            Console.WriteLine("will wait 5 sec then do get list again");
            await Task.Delay(5000);


            Console.WriteLine("Processor List");
            await GetAllProcessors(MgmtAppEndPoint);









        }

        public static async Task<string> getMgmtEndPoint(string sMgmtAppInstanceName)
        {
            FabricClient fc = new FabricClient(s_FabricEndPoint);
            var partition = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(sMgmtAppInstanceName));

            return partition.GetEndpoint().Address;
        }
        
        public static async Task<Processor> AddProcessor(string baseAddress, Processor processor)
        {
            var response = await IoTManagementTestLib.MgmtAddProcessor(baseAddress, processor);
            await formatResponse("Add", response);
            return JsonConvert.DeserializeObject<Processor>(await response.Content.ReadAsStringAsync());
        }
        public static async Task DeleteProcessor(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtDeleteProcessor(baseAddress, ProcessorName);
            await formatResponse("Delete", response);
        }

        public static async Task GetProcessor(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtGetProcessor(baseAddress, ProcessorName);
            await formatResponse("GetOne", response);
        }

        public static async Task GetAllProcessors(string baseAddress)
        {
            var response = await IoTManagementTestLib.MgmtGetAllPrceossors(baseAddress);
            await formatResponse("Getall", response);
        }

        // Per Processors Action

        public static async Task DrainStopProcessor(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtDrainStopProcessor(baseAddress, ProcessorName);
            await formatResponse("Drain Stop Processor", response);
        }

        public static async Task PauseProcessor(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtPauseProcessor(baseAddress, ProcessorName); ;
            await formatResponse("Pause Processor", response);
        }

        public static async Task ResumeProcessor(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtResumeProcessor(baseAddress, ProcessorName); ;
            await formatResponse("Resume Processor", response);
        }

        public static async Task GetProcessorStatus(string baseAddress, string ProcessorName)
        {
            var response = await IoTManagementTestLib.MgmtGetWorkerProcessor(baseAddress, ProcessorName); 
            await formatResponse("Get Processor Status", response);
        }

        #endregion
    }
}
