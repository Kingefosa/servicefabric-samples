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
    public class SendParams
    {
       public  int NumOfMessages;
        public string PublisherName;
        public string MessageFormat;
    }
    public static class IoTManagementTestLib
    {
        private static async Task doPreProcessing(HttpClient client)
        {
            // todo: wireup authN headers 
            await Task.Delay(0);
        }


        #region Per Worker Action
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


        public static async Task<HttpResponseMessage> MgmtGetWorkerProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/detailed", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }

#endregion




        public static  async Task<HttpResponseMessage> MgmtAddProcessor(string BaseAddress, Processor processor)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processor.Name));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmtGetAllPrceossors(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await client.SendAsync(message);
        }


        public static async Task<HttpResponseMessage> MgmtGetProcessor(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processorName));
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

        public static Task sendMessages(string EventHubConnectionString, 
                                             string EventHubName, 
                                             string PublisherNameFormat,  
                                             string Message,
                                             int NumOfMessages, 
                                             int NumOfPublishers)
        {
            var NumofMessagesPerClients = NumOfMessages / NumOfPublishers;
            var tasks = new List<Task>();

            for (int i = 1; i <= NumofMessagesPerClients; i++)
            {
                var P = new SendParams() {
                    NumOfMessages = NumofMessagesPerClients,
                    PublisherName = string.Format(PublisherNameFormat, i),
                    MessageFormat = Message
                };
                tasks.Add(Task.Factory.StartNew(async(p) =>
                {
                    var sendParameters = p as SendParams;
                    var eventHubClient = EventHubClient.CreateFromConnectionString(EventHubConnectionString, EventHubName);
                    var current = 1;

                    do
                    {
                        Trace.WriteLine(string.Format("sending message# {0} of {1} for publisher {2}", current, sendParameters.NumOfMessages, sendParameters.PublisherName));
                        var ev = new EventData(Encoding.UTF8.GetBytes(sendParameters.MessageFormat));
                        ev.SystemProperties[EventDataSystemPropertyNames.Publisher] = sendParameters.PublisherName;
                        await eventHubClient.SendAsync(ev);
                        current++;
                    }
                    while (current <= sendParameters.NumOfMessages);
                }
                    , P));
            }


            return Task.WhenAll(tasks);
        }
        




    }
}
