#define _VS_DEPLOY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using IoTGateway.Common;
using IoTGateway.Clients;


namespace WorkerSvc
{
    public class WorkerSvc : StatefulService
    {
        private static readonly string s_def_dictionary          = "defs";            // this dictionary<string, string> will be used to save assigned worker definition
        private static readonly string s_AssignedWorkerEntryName = "AssginedWorker";  // this is where we save assigned worker as json strong
        private static readonly string s_OwinListenerName        = "OwinListener";    // name of the OWIN listener key in Composite Listener

        public WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> WorkManager { get; private set; }

        private EventHubListenerDataHandler     m_EventHubListenerHandler = null; // every message on Event hub will be handled by this guy. 
        private CompositeCommunicationListener  m_Listener                = new CompositeCommunicationListener(); // one composite listener to rule them all
        private Worker m_AssignedWorker;  // each service will have an assigned worker which is a list of event hubs to pump data out of
    
        


        private async Task<Worker> GetAssignedWorkerFromState()
        {
            Worker worker = null; 
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            using (var tx = StateManager.CreateTransaction())
            {
                var cResult = await dict.TryGetValueAsync(tx, s_AssignedWorkerEntryName);
                if (cResult.HasValue)
                    worker = Worker.FromJsonString(cResult.Value);
                await tx.CommitAsync();
            }
            return worker;
        }
        private async Task SaveWorkerToState(Worker worker)
        {
            
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            using (var tx = StateManager.CreateTransaction())
            {
                var bUpdated = await dict.TryUpdateAsync(tx,s_AssignedWorkerEntryName, m_AssignedWorker.AsJsonString(),  worker.AsJsonString());
                if (!bUpdated)
                    throw new Exception("failed to udpate assigned worker!");

                await tx.CommitAsync();    
            }
            
        }
        private string HubDefToListenerName(EventHubDefinition HubDef)
        {
            return string.Concat(HubDef.EventHubName, "-", HubDef.ConnectionString, "-", HubDef.ConsumerGroupName);
        }

        /// <summary>
        /// ensures that composite lister has 1:1 event hub listener 
        /// assigned according to the assigned worker. it also that make sure 
        /// that Owin listener is not removed from the composite listener
        /// </summary>
        /// <returns></returns>
        private async Task RefreshListenersAsync()
        {
            // todo: find a cleaner way
            var toRemove = new List<string>();
            var worker = await GetAssignedWorkerAsync();


            // clear all (except the owin one)
            // we clear the event hub as we are not sure if the # of partitions has changed

            foreach (var kvp in m_Listener.Listners)
                if (kvp.Key != s_OwinListenerName)
                    await m_Listener.RemoveListenerAsync(kvp.Key);

            // Event hub communication listner uses a dictionary<string, string> for check points. 
            IReliableDictionary<string, string> LeaseStateDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            foreach (var hub in worker.Hubs)
            {

                var eventHubListener = new EventHubCommunicationListener(StateManager, // state manager used by eh listener for check pointing
                                                                         LeaseStateDictionary, // which dictionary will it use to save state it uses <string, string>
                                                                         hub.EventHubName, // which event hub will it pump messages out of
                                                                         hub.ConnectionString, // Service Bus connection string
                                                                         hub.ConsumerGroupName, // eh consumer group ("" => will use default consumer group).
                                                                         m_EventHubListenerHandler); // object that implements (IEventDataHandler) to be called by the listener when messages are recieved.


                await m_Listener.AddListenerAsync(HubDefToListenerName(hub), eventHubListener);
            }
        }


        public async Task<Worker> GetAssignedWorkerAsync()
        {
            
                // do we have it?
                if (null != m_AssignedWorker)
                    return m_AssignedWorker;

                //is it in state
                var worker = await GetAssignedWorkerFromState();
                if (worker != null)
                {
                    m_AssignedWorker = worker;      
                    return m_AssignedWorker;
                }

            // must be a new instance (or if we are debugging in VS.NET then use a manually created one)
#if _VS_DEPLOY


            var Worker1 = new Worker()
            {
                Name = "One"
            };
            Worker1.Hubs.Add(new EventHubDefinition()
            {
                ConnectionString = "",
                EventHubName = "eh01",
                ConsumerGroupName = ""
            });

            
            return Worker1;
#else
            if (null != ServiceInitializationParameters.InitializationData)
            {

                m_AssignedWorker = Worker.FromBytes(ServiceInitializationParameters.InitializationData);
                return m_AssignedWorker;
            }        
            
#endif
            throw new InvalidOperationException("Failed to load assigned worker from saved state and initialization data");

        }

        public async Task SetAssignedWorkerAsync(Worker newWorker)
        {
            await SaveWorkerToState( newWorker);
            await RefreshListenersAsync();
        }
        protected override ICommunicationListener CreateCommunicationListener()
        {

            //todo: add owin listener
            return m_Listener;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            
            WorkManager = new WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem>(this.StateManager);
            WorkManager.MaxNumOfWorkers = 1;// WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem>.s_Max_Num_OfWorker;


           await WorkManager.StartAsync();

            m_EventHubListenerHandler = new EventHubListenerDataHandler(WorkManager);

            await RefreshListenersAsync();
        }

        
    }
}
