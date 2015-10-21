// #define _VS_DEPLOY         // Used for debugging purposes, where the applicaiton is not created by ManagementService
// #define _WAIT_FOR_DEBUGGER // if defined the RunAsync() will wait for your debugger to be attached before moving forward
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using IoTProcessorManagement.Common;
using System.Diagnostics;
using IoTProcessorManagement.Clients;

namespace EventHubProcessor
{
    public class EventHubProcessorService : StatefulService
    {
        private static readonly string s_def_dictionary             = "defs";            // this dictionary<string, string> will be used to save assigned processor definition
        private static readonly string s_AssignedProcessorEntryName = "AssginedProcessor";  // this is where we save assigned processor as json strong
        private static readonly string s_OwinListenerName          = "OwinListener";    // name of the OWIN listener key in Composite Listener

        private WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem> WorkManager { get;  set; }
        private EventHubListenerDataHandler     m_EventHubListenerHandler = null; // every message on Event hub will be handled by this guy. 
        private Processor m_AssignedProcessor;  // each service will have an assigned Processor which is a list of event hubs to pump data out of
        private CompositeCommunicationListener m_CompositeListener; // one composite listener to rule them all

        private string m_ErrorMessage = string.Empty; // Error messages generated during listener creation.
        private bool m_IsInErrorState = false; // Error flag set in listener creation.

        public TraceWriter TraceWriter { get; private set; } // used to allow components to use Event Source/ServiceMessage


        public EventHubProcessorService()
        {
            TraceWriter        = new TraceWriter(this);
            m_CompositeListener  = new CompositeCommunicationListener(TraceWriter);


            m_CompositeListener.OnCreateListeningAddress = (listener, addresslist) => {
                // listener should return Owin Listener
                return addresslist.Single(kvp => kvp.Key == s_OwinListenerName).Value;
            };
        }

        #region Service Control & Telemetery
        // event hub listeners does not support pause and resume
        //so we just remove and recreate them.

        public Task Pause()
        {
            if (WorkManager.WorkManagerStatus != WorkManagerStatus.Working)
                throw new InvalidOperationException("can not pause service if its not in working state");

           return Task.Run(async () =>
           {
               await ClearEventHubListeners();
               await WorkManager.PauseAsync();
           });
        }
        public Task Resume()
        {
            if (WorkManager.WorkManagerStatus != WorkManagerStatus.Paused)
                throw new InvalidOperationException("can not resume service if its not in paused state");

            return Task.Run(async () =>
            {
                await WorkManager.ResumeAsync();
                await ClearEventHubListeners();
               
            });
        }
        public Task Stop()
        {
            if (WorkManager.WorkManagerStatus == WorkManagerStatus.Working 
                || 
                WorkManager.WorkManagerStatus == WorkManagerStatus.Paused
                ||
                WorkManager.WorkManagerStatus == WorkManagerStatus.Draining
                )
            {


                return Task.Run(async () =>
                {
                    await ClearEventHubListeners();
                    await WorkManager.StopAsync();
                });
            }
            else
            {
                throw new InvalidOperationException("can not stop service if its not in working or paused state");
            }


        }
        public Task DrainAndStop()
        {
            if (WorkManager.WorkManagerStatus == WorkManagerStatus.Working 
                || 
                WorkManager.WorkManagerStatus == WorkManagerStatus.Paused)
            {
                return Task.Run(async () =>
                {
                    if (WorkManager.WorkManagerStatus == WorkManagerStatus.Paused)
                        await Resume();


                    await ClearEventHubListeners();
                    await WorkManager.DrainAndStopAsync();
                });
            }
            else
            {
                throw new InvalidOperationException("can not drain stop service if its not in working or paused state");
            }            
        }
        public Task<int> GetNumberOfActiveQueuesAsync()
        {
            return Task.Run(() =>
            {
                return WorkManager.NumberOfActiveQueues;
            });
        }
        public Task<int> GetTotalPostedLastMinuteAsync()
        {
           return Task.Run(() =>
           {
               return WorkManager.TotalPostedLastMinute;
           });
        }
        public Task<int> GetTotalProcessedLastMinuteAsync()
        {
           return Task.Run(() =>
           {
                return WorkManager.TotalProcessedLastMinute;
           });
        }
        public Task<int> GetTotalPostedLastHourAsync()
        {
           return Task.Run(() =>
           {
                return WorkManager.TotalPostedLastHour;
            });
        }
        public Task<int> GetTotalProcessedLastHourAsync()
        {
           return Task.Run(() =>
           {
                return WorkManager.TotalProcessedLastHour;
           });
        }
        public Task<float> GetAveragePostedPerMinLastHourAsync()
        {
           return Task.Run(() =>
           {
                return WorkManager.AveragePostedPerMinLastHour;
           });

        }
        public Task<float> GetAverageProcessedPerMinLastHourAsync()
        {
           return Task.Run(() =>
           {
                return WorkManager.AverageProcessedPerMinLastHour;
            });

        }
        public Task<string> GetStatusStringAsync()
        {
            // instead of maintaining a new enum for processor service status
            // we are using the work manager status since this service is not 
            // doing anything other than posting and managing work items. 
            return Task.FromResult(WorkManager.WorkManagerStatus.ToString());
        }

        public Task<long> GetNumOfBufferedItemsAsync()
        {
            return Task.FromResult(WorkManager.NumberOfBufferedWorkItems);
        }

        
        public string ErrorMessage
        {
            get { return m_ErrorMessage; }
        }
        public bool IsInErrorState
        {
            // we maintain a seprate error flag because while worker might be in 
            // working state, processing buffered items, listener might be in error state.
            get { return m_IsInErrorState; }
        } 
       

        // updates the current assigned processor (the # of event hubs)
        public async Task SetAssignedProcessorAsync(Processor newProcessor)
        {
            // save the processor (replacing whatever we had)
            m_AssignedProcessor =  await SaveProcessorToState(newProcessor);

            // if we are in working mode, refresh listeners
            if(WorkManager.WorkManagerStatus == WorkManagerStatus.Working)
                await RefreshListenersAsync();
        }
        
        #endregion
        #region Listeners Management 

        private async Task ClearEventHubListeners()
        {
            // clear all (except the owin one)
            // we clear the event hub as we are not sure if the # of partitions has changed

            foreach (var kvp in m_CompositeListener.Listners)
                if (kvp.Key != s_OwinListenerName)        
                    await m_CompositeListener.RemoveListenerAsync(kvp.Key);
            
        }
        private async Task RefreshListenersAsync()
        {
            m_IsInErrorState = false;
            m_ErrorMessage = string.Empty;

            var processor = await GetAssignedProcessorAsync();
            TraceWriter.TraceMessage(string.Format("Begin Refresh Listeners, creating {0} event hub listeners", processor.Hubs.Count));

            // since we don't keep track of event hub config, connections
            // partitions count etc. we will assume that they have *all* changed 
            // so we will remove all active Event Hub listeners and add the new ones. 
            await ClearEventHubListeners();

            // Event hub communication listner uses a dictionary<string, string> for check points. 
            // Event Hub Event listener (using Event Processor Approach) implementation uses IReliableState to store
            // checkpoint https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.lease.aspx
            // the leases uses consistent naming, hence even if we removed a listener and added it will pick
            // up from where it lift exactly (in terms of Event Hub Sequence #)
            IReliableDictionary<string, string> LeaseStateDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            TraceWriter.TraceMessage(string.Format("Event Hub Leases are saved in reliable dictionary named {0}", s_def_dictionary));

            foreach (var hub in processor.Hubs)
            {
                var ListenerName = HubDefToListenerName(hub);
                var BadListener = false;
                var sErrorMessage = string.Empty;

                try
                {
                    var eventHubListener = new EventHubCommunicationListener(TraceWriter,
                                                                             StateManager, // state manager used by eh listener for check pointing
                                                                             LeaseStateDictionary, // which dictionary will it use to save state it uses IReliableDictionary<string, string>
                                                                             hub.EventHubName, // which event hub will it pump messages out of
                                                                             hub.ConnectionString, // Service Bus connection string
                                                                             hub.ConsumerGroupName, // eh consumer group ("" => will use default consumer group).
                                                                             m_EventHubListenerHandler, // object that implements (IEventDataHandler) to be called by the listener when messages are recieved.
                                                                             EventHubCommunicationListenerMode.Distribute,  // refer to EventHubCommunicationListenerMode 
                                                                             string.Empty // no particular event hub partition is assigned to this replica, it will be auto assigned
                                                                             );

                    await m_CompositeListener.AddListenerAsync(ListenerName, eventHubListener);
                }
                
                catch (AggregateException aex)
                {
                    BadListener = true;

                    var ae = aex.Flatten();
                    sErrorMessage = string.Format("Event Hub Listener for Connection String:{0} Hub:{1} CG:{2} generated an error, other listeners will keep on running and replica will enter error state. E:{3} StackTrace:{4}",
                                                          hub.ConnectionString, hub.EventHubName, hub.ConsumerGroupName, ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace());


                }
                catch (Exception e)
                {
                    BadListener = true;
                    sErrorMessage = string.Format("Event Hub Listener for Connection String:{0} Hub:{1} CG:{2} generated an error, other listeners will keep on running and replica will enter error state. E:{3} StackTrace:{4}",
                                      hub.ConnectionString, hub.EventHubName, hub.ConsumerGroupName, e.Message, e.StackTrace);
                    
                }
                finally
                {
                    if (BadListener)
                    {
                        try { await m_CompositeListener.RemoveListenerAsync(ListenerName); } catch { /* no op*/}
                        TraceWriter.TraceMessage(sErrorMessage);
                        m_IsInErrorState = true;
                        m_ErrorMessage = string.Concat(m_ErrorMessage, "\n", sErrorMessage);
                    }




                }

            }

                TraceWriter.TraceMessage("End Refresh Listeners");
        }

        #endregion
        #region Assigned Processor State and Change Management 

        private async Task<Processor> GetAssignedProcessorFromState()
        {
            Processor processor = null; 
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            using (var tx = StateManager.CreateTransaction())
            {
                var cResult = await dict.TryGetValueAsync(tx, s_AssignedProcessorEntryName);
                if (cResult.HasValue)
                    processor = Processor.FromJsonString(cResult.Value);
                await tx.CommitAsync();
            }
            return processor;
        }
        private async Task<Processor> SaveProcessorToState(Processor processor)
        {
            
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(s_def_dictionary);

            var sValue = processor.AsJsonString();
            using (var tx = StateManager.CreateTransaction())
            {
                await dict.AddOrUpdateAsync(tx, 
                                            s_AssignedProcessorEntryName, 
                                            sValue,
                                            (k,v) => { return sValue; });
                
                await tx.CommitAsync();    
            }
            return processor;
        }
        private string HubDefToListenerName(EventHubDefinition HubDef)
        {
            return string.Concat(HubDef.EventHubName, "-", HubDef.ConnectionString, "-", HubDef.ConsumerGroupName);
        }



        public async Task<Processor> GetAssignedProcessorAsync()
        {
            
                // do we have it?
                if (null != m_AssignedProcessor)
                    return m_AssignedProcessor;

                //is it in state
                var processor = await GetAssignedProcessorFromState();
                if (processor != null)
                {
                    m_AssignedProcessor = processor;      
                    return m_AssignedProcessor;
                }

            // must be a new instance (or if we are debugging in VS.NET then use a manually created one)
#if _VS_DEPLOY
            // in this mode we load a mock up Processor and use it.
            // this mode is used only during single processor (set as a startup)
            // project 
            TraceWriter.TraceMessage("Processor is running in VS.NET Deploy Mode");

            var processor1 = new Processor()
            {
                Name = "One"
            };
            processor1.Hubs.Add(new EventHubDefinition()
            {
                ConnectionString = "//Event Hub connection string here //",
                EventHubName = "eh01",
                ConsumerGroupName = ""
            });

            
            return processor1;
#else
            if (null != ServiceInitializationParameters.InitializationData)
            {

                var initProcessor = Processor.FromBytes(ServiceInitializationParameters.InitializationData);
                Trace.WriteLine(string.Format(string.Format("Replica {0} Of Application {1} Got Processor {2}",
                                                            ServiceInitializationParameters.ReplicaId,
                                                            ServiceInitializationParameters.CodePackageActivationContext.ApplicationName,
                                                            initProcessor.Name)));

                m_AssignedProcessor = await SaveProcessorToState(initProcessor); // this sets m_assignedprocessor

                return m_AssignedProcessor;
            }        
            
#endif
            throw new InvalidOperationException("Failed to load assigned processor from saved state and initialization data");

        }

      
        #endregion



        protected override ICommunicationListener CreateCommunicationListener()
        {
            /*
                the listener is created as a member variable of service object. 
                the listener will get 1..n of Event Hub listeners according to ProcessorDefinition
                and one -only one - OWIN listener that repsent the control endpoint

                when the we get a request to create commnuication listener we just add to 
                whatever in the composite listener a new Owin listener
            */
            var spec = new ProcessorServiceOwinListenerSpec(); // in addition to standard Owin stuff  
                                                        //this Specification class injects a service reference in every
                                                        // API controller instance created
            spec.Svc = this;
            m_CompositeListener.AddListenerAsync(s_OwinListenerName, new OwinCommunicationListener(spec)).Wait();


            return m_CompositeListener;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
#if _WAIT_FOR_DEBUGGER
            while (!Debugger.IsAttached)
                await Task.Delay(5000);
#endif

            // Create a work manager that is expected to receive a work item of type RouteToActor
            // The work manager will queue them (according to RouteToActor.Queue Name property). Then for each work 
            //item de-queued it will use RouteToActorWorkItemHandler type to process the work item
            WorkManager = new WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem>(this.StateManager, TraceWriter);


            // work manager will creates a new (RoutetoActorWorkItemHandler) 
            // per queue. our work item handling is basically forwarding event hub message to the Actor. 
            // since each Actor will have it is own queue (and a handler). each handler
            // can cache a reference to the actor proxy instead of caching them at a higher level
            WorkManager.WorkItemHandlerMode = WorkItemHandlerMode.PerQueue;

            // maximum # of worker loops (loops that de-queue from reliable queue)
            WorkManager.MaxNumOfWorkers = WorkManager<RoutetoActorWorkItemHandler, RouteToActorWorkItem>.s_Max_Num_OfWorker;

            WorkManager.YieldQueueAfter = 50; // worker will attempt to process
                                              // 50 work item per queue before dropping it 
                                              // and move to the next. 

            // if a queue stays empty more than .. it will be removed
            WorkManager.RemoveEmptyQueueAfter = TimeSpan.FromSeconds(10);
            
                // start it
            await WorkManager.StartAsync();

            // this wire up Event hub listeners that uses EventHubListenerDataHandler to 
            // post to WorkManager which then (create or get existing queue) then en-queue.
            m_EventHubListenerHandler = new EventHubListenerDataHandler(WorkManager);

            // this ensures that an event hub listener is created
            // per every assigned event hub
            await RefreshListenersAsync();

            while (!cancellationToken.IsCancellationRequested)
                await Task.Delay(5000);


            TraceWriter.TraceMessage("Replica is existing, stopping the work manager");

            try
            {
                await ClearEventHubListeners();
                await WorkManager.StopAsync();
            }
            catch (AggregateException aex)
            {
                var ae = aex.Flatten();
                TraceWriter.TraceMessage(string.Format("as the replica unload (run async canceled) the followng errors occured E:{0} StackTrace:{1}", aex.GetCombinedExceptionMessage(), aex.GetCombinedExceptionStackTrace()));
            }            
        }

        
    }
}
