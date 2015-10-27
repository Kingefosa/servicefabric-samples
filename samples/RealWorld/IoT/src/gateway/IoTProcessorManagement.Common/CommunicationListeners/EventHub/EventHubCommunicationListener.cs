// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Diagnostics;

namespace IoTProcessorManagement.Common
{
    public class EventHubCommunicationListener: ICommunicationListener
    {
        public readonly string EventHubName;
        public readonly string EventHubConnectionString;
        public readonly IEventDataHandler Handler;
        public readonly string EventHubConsumerGroupName;
        public readonly IReliableDictionary<string, string> StateDictionary;
        public readonly IReliableStateManager StateManager;
        public readonly string EventHubPartitionId;


        public EventHubCommunicationListenerMode ListenerMode { get; private set; }


        private MessagingFactory m_MessagingFactory;
        private EventHubClient m_EventHubClient;
        private EventHubConsumerGroup m_ConsumerGroup;
        private EventProcessorFactory m_EventProcessorFactory;

        private string m_Namespace;
        private StatefulServiceInitializationParameters m_InitParams;
        private ITraceWriter m_TraceWriter;
        private string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(m_Namespace))
                {
                    
                    string[] elements = EventHubConnectionString.Split(';');

                    foreach (var elem in elements)
                        if (elem.ToLowerInvariant().StartsWith("endpoint="))
                            m_Namespace = new Uri(elem.Split('=')[1]).Host;
                        

                }
                return m_Namespace;
            }
        }


        private async Task<string[]> getOrderedServicePartitionIds()
        {
            var fabricClient = new FabricClient();
            var PartitionList = await fabricClient.QueryManager.GetPartitionListAsync(m_InitParams.ServiceName);

             var partitions = new List<string>();

            foreach (var p in PartitionList)
                partitions.Add(p.PartitionInformation.Id.ToString());


            return partitions.OrderBy(s => s).ToArray();
        }

        private string[] DisributeOverServicePartitions(string[] orderEventHubPartition, string[] orderServicePartitionIds)
        {

            // service partitions are greater or equal
            // in this case each service partition gets an event hub partitions
            // the reminder partitions will just not gonna work on anything. 
            if (orderServicePartitionIds.Length >= orderEventHubPartition.Length)
            {
                int servicePartitionRank = Array.IndexOf(orderServicePartitionIds, m_InitParams.PartitionId.ToString());

                return new string[] { orderEventHubPartition[servicePartitionRank] };
            }
            else
            {
                // service partitions are less than event hub partitins, distribute.. 
                // service partitions can be odd or even. 

                var reminder = orderEventHubPartition.Length % orderServicePartitionIds.Length;
                int HubPartitionsPerServicePartitions = orderEventHubPartition.Length / orderServicePartitionIds.Length;
                int servicePartitionRank = Array.IndexOf(orderServicePartitionIds, m_InitParams.PartitionId.ToString());

                var assignedIds = new List<string>();
                for (var i = 0; i < HubPartitionsPerServicePartitions; i++)
                    assignedIds.Add(orderEventHubPartition[(servicePartitionRank * HubPartitionsPerServicePartitions) + i]);

                // last service partition gets the reminder
                if(servicePartitionRank == (orderServicePartitionIds.Length - 1))
                    for (var i = reminder; i > 0; i--)
                        assignedIds.Add(orderEventHubPartition[orderEventHubPartition.Length - i]);

                return assignedIds.ToArray();
            }
        }
        private async Task<string[]>  ResolveEventHubPartitions(string[] PartitionIds)
        {
            var OrderIds = PartitionIds.OrderBy((s) => s).ToArray();


            switch (ListenerMode)
            {

                case EventHubCommunicationListenerMode.Single:
                {
                    if (!OrderIds.Contains(EventHubPartitionId))
                        throw new InvalidOperationException(string.Format("Event hub Partition {0} is not found", EventHubPartitionId));

                    return new string[] { EventHubPartitionId };
                }
                case EventHubCommunicationListenerMode.OneToOne:
                {
                        var servicePartitions = await getOrderedServicePartitionIds();
                        if (servicePartitions.Length != OrderIds.Length)
                            throw new InvalidOperationException("Event Hub listener is in 1:1 mode yet servie partitions are not equal to event hub partitions");

                        int servicePartitionRank = Array.IndexOf(servicePartitions, m_InitParams.PartitionId.ToString());

                        return new string[] { OrderIds[servicePartitionRank] };
                }
                case EventHubCommunicationListenerMode.Distribute:
                {

                        var servicePartitions = await getOrderedServicePartitionIds();
                        return DisributeOverServicePartitions(OrderIds, servicePartitions);
                }
                case EventHubCommunicationListenerMode.SafeDistribute:
                {
                        var servicePartitions = await getOrderedServicePartitionIds();
                        // we can work with service partitions < or = Event Hub partitions 
                        // anything else is an error case

                        if(servicePartitions.Length > OrderIds.Length)
                            throw new InvalidOperationException("Event Hub listener is in fairDistribute mode yet servie partitions greater than event hub partitions");

                        return DisributeOverServicePartitions(OrderIds, servicePartitions);
                }
                default:
                {
                        throw new InvalidOperationException(string.Format("can not resolve event hub partition for {0}", ListenerMode.ToString()));
                }
        }

            




            throw new InvalidOperationException("could not resolve event hub partitions");
        }
#region ctors
        public EventHubCommunicationListener(ITraceWriter TraceWriter,
                                     IReliableStateManager stateManager,
                                     IReliableDictionary<string, string> stateDictionary,
                                     string eventHubName,
                                     string eventHubConnectionString,
                                     string eventHubConsumerGroupName,
                                     IEventDataHandler handler, 
                                     EventHubCommunicationListenerMode Mode,
                                     string eventHubPartitionId) 
        {
            ListenerMode = Mode;
            if (ListenerMode == EventHubCommunicationListenerMode.Single && string.IsNullOrEmpty(eventHubPartitionId))
                throw new InvalidOperationException("Event hub communication listener in single mode requires a partition id");


            m_TraceWriter = TraceWriter;

            EventHubName = eventHubName;
            EventHubConnectionString = eventHubConnectionString;
            Handler = handler;
            EventHubConsumerGroupName = eventHubConsumerGroupName;
            StateManager = stateManager;
            StateDictionary = stateDictionary;
            ListenerMode = Mode;


            m_TraceWriter.TraceMessage(string.Format("Event Hub Listener created for {0} on {1} group:{2} mode:{3}", EventHubName, this.Namespace, EventHubConsumerGroupName, ListenerMode.ToString()));
        }


        public EventHubCommunicationListener(ITraceWriter TraceWriter, 
                                             IReliableStateManager stateManager,
                                             IReliableDictionary<string, string> stateDictionary,
                                             string eventHubName, 
                                             string eventHubConnectionString,
                                             string eventHubConsumerGroupName,
                                             IEventDataHandler handler)  : this (TraceWriter,
                                                                                 stateManager, 
                                                                                 stateDictionary, 
                                                                                 eventHubName, 
                                                                                 eventHubConnectionString, 
                                                                                 eventHubConsumerGroupName, 
                                                                                 handler,
                                                                                 EventHubCommunicationListenerMode.SafeDistribute, 
                                                                                 string.Empty)
        {
            
                
        }


#endregion
        public void Abort()
        {
            if(null != m_MessagingFactory && !m_MessagingFactory.IsClosed)
                m_MessagingFactory.Close();

            
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if(null != m_MessagingFactory && !m_MessagingFactory.IsClosed)
                await m_MessagingFactory.CloseAsync();

            m_TraceWriter.TraceMessage(string.Format("Event Hub Listener for {0} on {1} closed", EventHubName, this.Namespace));

        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            StatefulServiceInitializationParameters initParams = serviceInitializationParameters as StatefulServiceInitializationParameters;

            if (null == initParams)
                throw new InvalidOperationException("Event Hub Communication Listener can only run in  stateful service");


            m_InitParams = initParams;
            m_TraceWriter.TraceMessage(string.Format("Event Hub Listener for {0} on {1} initialized", EventHubName, this.Namespace));

        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {

            m_MessagingFactory = MessagingFactory.CreateFromConnectionString(EventHubConnectionString);
            m_EventHubClient = m_MessagingFactory.CreateEventHubClient(EventHubName);
            m_ConsumerGroup = !string.IsNullOrEmpty(EventHubConsumerGroupName) ?
                                m_EventHubClient.GetConsumerGroup(EventHubConsumerGroupName)
                                :
                                m_EventHubClient.GetDefaultConsumerGroup();


            // slice the pie according to distribution
            // this partition can get one or more assigned Event Hub Partition ids
            var EventHubPartitionIds = m_EventHubClient.GetRuntimeInformation().PartitionIds;
            var assignedPartitionsIds = await ResolveEventHubPartitions(EventHubPartitionIds);

            m_EventProcessorFactory = new EventProcessorFactory(Handler, this.EventHubName, this.Namespace, this.EventHubConsumerGroupName);
            var checkPointManager = new CheckPointManager();



            m_TraceWriter.TraceMessage(string.Format("Event Hub Listener for {0} on {1} using mode:{2} handling:{3}/{4} event hub partitions", 
                                                      EventHubName, 
                                                      this.Namespace, 
                                                      ListenerMode, 
                                                      assignedPartitionsIds.Count(),
                                                      EventHubPartitionIds.Count()));


            foreach ( var pid in assignedPartitionsIds)
            {
                var lease =  await StateManagerLease.GetOrCreateAsync(StateManager,
                                                                      StateDictionary,
                                                                      m_Namespace,
                                                                      EventHubConsumerGroupName,
                                                                      EventHubName, 
                                                                      pid);


                await m_ConsumerGroup.RegisterProcessorFactoryAsync(lease,
                                                                    checkPointManager,
                                                                    m_EventProcessorFactory);

            }


            return string.Concat(this.EventHubName, " @ " , this.Namespace);
        }
    }
}
