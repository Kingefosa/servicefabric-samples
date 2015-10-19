using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.TestLib
{
    public static class IoTEventHubSender
    {
        private class SendParams
        {
            public int NumOfMessages;
            public string PublisherName;
        }

        public static Task SendEventHubMessages(string EventHubConnectionString,
                                               string EventHubName,
                                               int NumOfMessages,
                                               int NumOfPublishers)
        {
            var PublisherNameFormat = "sensor{0}";
            var NumofMessagesPerClient = NumOfMessages / NumOfPublishers;
            var tasks = new List<Task>();

            for (int i = 1; i <= NumOfPublishers; i++)
            {
                var P = new SendParams()
                {
                    NumOfMessages = NumofMessagesPerClient,
                    PublisherName = string.Format(PublisherNameFormat, i)
                };
                tasks.Add(Task.Factory.StartNew(async (p) =>
                                                {
                                                    var sendParameters = p as SendParams;
                                                    var eventHubClient = EventHubClient.CreateFromConnectionString(EventHubConnectionString, EventHubName);
                                                    var current = 1;
                                                    var rand = new Random();

                                                    do
                                                    {

                                                        var Event = new
                                                        {
                                                            DeviceId =sendParameters.PublisherName,
                                                            BuildingId = "b1",
                                                            TempF = rand.Next(1, 100).ToString(),
                                                            Humidity = rand.Next(1, 100).ToString(),
                                                            Motion = rand.Next(1, 10).ToString(),
                                                            light = rand.Next(1, 10).ToString(),
                                                            EventDate = DateTime.UtcNow.ToString()
                                                        };

                                                        Trace.WriteLine(string.Format("sending message# {0} of {1} for publisher {2}", current, sendParameters.NumOfMessages, sendParameters.PublisherName));
                                                        var ev = new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Event)));
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
