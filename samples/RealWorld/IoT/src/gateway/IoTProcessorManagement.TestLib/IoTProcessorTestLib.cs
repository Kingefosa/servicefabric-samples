using IoTProcessorManagement.Clients;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.TestLib
{
    
    public static class IoTProcessorTestLib
    {
        private static async Task doPreProcessing(HttpClient client)
        {
            // todo: wireup authN headers 
            await Task.Delay(0);
        }


        public static async Task<HttpResponseMessage> doDrainStop(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "eventhubprocessor/drainstop"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static  async Task<HttpResponseMessage> doPause(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "eventhubprocessor/pause"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> doResume(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "eventhubprocessor/resume"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> doGetProcessorStatus(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "eventhubprocessor/"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }
    }
}
