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

namespace IoTProcessorManagement
{
   

   

    static class InternalFunctions
    {
#if DEBUG
        /*
            the following function should be removed from the final deployment
            it is only used to send test messages to event hub
        */

            

        private static async Task SendToEventsToEventHubAsync(int NumberOfMessages, 
                                                              string PublisherName, 
                                                              EventHubDefinition HubDefinition)
        {

                var EventHubConnectionString = HubDefinition.ConnectionString;
                var EventHubName = HubDefinition.EventHubName;

                var eventHubClient = EventHubClient.CreateFromConnectionString(EventHubConnectionString, EventHubName);
                var current = 1;
                var rand = new Random();

                do
                {

                    var Event = new
                    {
                        DeviceId = PublisherName,
                        BuildingId = "b1",
                        TempF = rand.Next(1, 100).ToString(),
                        Humidity = rand.Next(1, 100).ToString(),
                        Motion = rand.Next(1, 10).ToString(),
                        light = rand.Next(1, 10).ToString(),
                        EventDate = DateTime.UtcNow.ToString()
                    };

                // Powershell redirects stdout to PS console.
                    Console.WriteLine(string.Format("sending message# {0}/{1} for Publisher {2} Hub:{3}", current, NumberOfMessages, PublisherName, HubDefinition.EventHubName));
                    var ev = new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Event)));
                    ev.SystemProperties[EventDataSystemPropertyNames.Publisher] = PublisherName;
                    await eventHubClient.SendAsync(ev);
                    current++;
                }
                while (current <= NumberOfMessages);
            
        }
        public static async Task SendTestEventsToProcessorHubsAsync(
                                                            Processor processor, 
                                                            int NumOfMessages,
                                                            int NumOfPublishers)
        {

            var PublisherNameFormat = "sensor{0}";
            var NumberOfMessagesPerPublisher = NumOfMessages / NumOfPublishers;
            var tasks = new List<Task>();

            for (int i = 1; i <= NumOfPublishers; i++)
            {
                foreach (var hub in processor.Hubs)
                {
                    var publisherName = string.Format(PublisherNameFormat, i);
                    tasks.Add(SendToEventsToEventHubAsync(NumberOfMessagesPerPublisher, publisherName, hub));
                }
            }
            await Task.WhenAll(tasks);
        }

#endif

        #region helpers
        private static async Task<Processor> GetHttpResponseAsProcessorAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Processor>(sJson);
        }

        private static async Task<Processor[]> GetHttpResponseAsProcessorsAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Processor[]>(sJson);
        }
        private static async Task<ProcessorRuntimeStatus[]> GetHttpResponseAsRuntimeStatusAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var sJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ProcessorRuntimeStatus[]>(sJson);
        }

        private static async Task doPreProcessing(HttpClient client)
        {
            // todo: wireup authN headers 
            await Task.Delay(0);
        }
#endregion
        public static async Task<string> GetManagementApiEndPointAsync(string FabricEndPoint, string sMgmtAppInstanceName)
        {
            FabricClient fc = new FabricClient(FabricEndPoint);
            var partition = await fc.ServiceManager.ResolveServicePartitionAsync(new Uri(sMgmtAppInstanceName));

            return partition.GetEndpoint().Address;
        }




        #region Per Worker Action
        public static async Task<Processor> StopProcessorAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/stop", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }
        public static async Task<Processor> DrainStopProcessorAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/drainstop", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }


        public static async Task<Processor> PauseProcessorAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/pause", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }


        public static async Task<Processor> ResumeProcessorAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/resume", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }


        public static async Task<ProcessorRuntimeStatus[]> GetDetailedProcessorStatusAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, string.Format("processor/{0}/detailed", processorName)));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await GetHttpResponseAsRuntimeStatusAsync(await client.SendAsync(message));
        }

        #endregion



        public static async Task<Processor> UpdateProcessorAsync(string BaseAddress, Processor processor)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processor.Name));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Put, uri);
            message.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }


        

        public static  async Task<Processor> AddProcessorAsync(string BaseAddress, Processor processor)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processor.Name));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(JsonConvert.SerializeObject(processor), Encoding.UTF8, "application/json");
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }



        public static async Task<Processor> GetProcessorAsync(string BaseAddress, string ProcessorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", ProcessorName));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }


        public static async Task<Processor[]> GetAllProcesserosAsync(string BaseAddress)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/"));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            return await GetHttpResponseAsProcessorsAsync(await client.SendAsync(message));
        }


        



        public static async Task<Processor> DeleteProcessorAsync(string BaseAddress, string processorName)
        {
            var uri = new Uri(string.Concat(BaseAddress, "processor/", processorName));
            var client = new HttpClient();
            await doPreProcessing(client);

            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            return await GetHttpResponseAsProcessorAsync(await client.SendAsync(message));
        }

       
        




    }
}
