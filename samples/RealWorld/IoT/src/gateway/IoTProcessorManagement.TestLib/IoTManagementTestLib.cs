using IoTProcessorManagement.Clients;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.TestLib
{
   

   

    public static class IoTManagementTestLib
    {
        private static async Task doPreProcessing(HttpClient client)
        {
            // todo: wireup authN headers 
            await Task.Delay(0);
        }
        #region PowerShell Helpers
        

        public static async Task<Processor> GetHttpResponseAsProcessor(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Processor>(sJson);
        }

        public static async Task<Processor[]> GetHttpResponseAsProcessors(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<Processor[]>(sJson);
        }
        public static async Task<ProcessorRuntimeStatus[]> GetHttpResponseAsRuntimeStatus(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ProcessorRuntimeStatus[]>(sJson);
        }


        public static async Task<HttpResponseMessage> MgmtUpdateProcessor(string BaseAddress, string processorJson)
        {
            var processor = Processor.FromJsonString(processorJson);
            return await MgmtUpdateProcessor(BaseAddress, processor);
        }




        public static async Task<HttpResponseMessage> MgmtAddProcessor(string BaseAddress, string processorJson)
        {
            var processor = Processor.FromJsonString(processorJson);
            return await MgmtAddProcessor(BaseAddress, processor);
        }



        #endregion
        public static async Task<string> getMgmtEndPoint(string FabricEndPoint, string sMgmtAppInstanceName)
        {
            FabricClient fc = new FabricClient(FabricEndPoint);
            var partition = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(sMgmtAppInstanceName));

            return partition.GetEndpoint().Address;
        }




        #region Per Worker Action
        public static async Task<HttpResponseMessage> MgmtStopProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/stop", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }
        public static async Task<HttpResponseMessage> MgmtDrainStopProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/drainstop", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmtPauseProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/pause", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmtResumeProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/resume", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmtGetDetailedProcessorStatus(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/detailed", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }

        #endregion



        public static async Task<HttpResponseMessage> MgmtUpdateProcessor(string BaseAddress, Processor processor)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processor.Name));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Put, uri);
            message.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            return await client.SendAsync(message);
        }


        

        public static  async Task<HttpResponseMessage> MgmtAddProcessor(string BaseAddress, Processor processor)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processor.Name));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            return await client.SendAsync(message);
        }



        public static async Task<HttpResponseMessage> MgmtGetPrcossor(string BaseAddress, string ProcessorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", ProcessorName));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmgGetAllProcesseros(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }


        



        public static async Task<HttpResponseMessage> MgmtDeleteProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processorName));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            return await client.SendAsync(message);
        }

       
        




    }
}
