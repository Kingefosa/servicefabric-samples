// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// CQRS implementation that uses multiple IReliableQueue<WorkItem> as storage.
    /// Work Manager performs:
    /// 0- Posting to work manager
    /// 1- Fan out based on Workitem.QueueName
    /// 2- Concurrent Execution on Queues. 
    /// 3- Fair execution, each queue gets a slice (# of dequeue before yeilding)
    /// 4- Does not allow executers to compete on one queue. 1:1 queue to executer 
    /// at any point of time. 
    /// 5- Supports Handler Management
    /// 6- Queue graceful removal 
    /// </summary>
    /// <typeparam name="Handler"></typeparam>
    /// <typeparam name="Wi"></typeparam>
    public partial class WorkManager<Handler, Wi> where Handler : IWorkItemHandler<Wi> , new() 
                                                  where Wi : IWorkItem
                                           
    {
 


        // defaults
        public static readonly string s_Queue_Names_Dictionary = "_QueueNames_"; // the dictionary that will hold a list (per entry) of each queue created. 
        public static readonly uint s_Default_Yield_Queue_After             = 10; // to ensure fairness, each queue will be de-queued 10 times before moving to the next.
        public static readonly uint s_Max_Num_OfWorker              = (uint) Environment.ProcessorCount * 2; // default is one task per processor, note: this does not mean affinity or what so ever.
        public static readonly uint s_MaxNumOfBufferedWorkItems     = 10  * 1000;
        public static readonly uint s_defaultMaxNumOfWorkers        = 2;


#region Specs 
        private WorkItemHandlerMode m_WorkItemHandlerMode   = WorkItemHandlerMode.Singlton; // defines how handlers are and mapped to queues
        private WorkManagerStatus m_WorkManagerStatus       = WorkManagerStatus.New;        
        // starting # of max workers
        private uint m_MaxNumOfWorkers                      = s_defaultMaxNumOfWorkers;
        // Maximum allowed work item in *all queues*
        private uint m_MaxNumOfBufferedWorkItems            = s_MaxNumOfBufferedWorkItems;
        // each worker will pump and yield after
        private uint m_Yield_Queue_After                    = s_Default_Yield_Queue_After;
        // remove queue if stayeed empty for? 
        private long m_RemoveEmptyQueueAfterTicks            = TimeSpan.FromMinutes(1).Ticks;
        #endregion

        private ITraceWriter m_TraceWriter                       = null; // used to write trace events. 
        private long m_NumOfBufferedWorkItems                    = 0;    // current num of buffered items
        private QueueManager<Handler, Wi> m_QueueManager         = null; // queue manager (saves queues names in state).
        private IReliableStateManager m_StateManager             = null; // all state is saved there (actual queues & and thier names managed by QueueManager)
        private ConcurrentDictionary<string, Handler> m_handlers = new ConcurrentDictionary<string, Handler>(); // list of handlers
        private ConcurrentDictionary<string, WorkExecuter<Handler, Wi>> m_Executers = new ConcurrentDictionary<string, WorkExecuter<Handler, Wi>>(); // worker tasks.
        private DeferredTaskExecuter m_DeferedTaskExec = new DeferredTaskExecuter(); // supports limited background processing. 

        private Clicker<WorkManagerClick, int> m_MinuteClicker = new Clicker<WorkManagerClick, int>(TimeSpan.FromMinutes(1));
        private Clicker<WorkManagerClick, int> m_HourClicker = new Clicker<WorkManagerClick, int>(TimeSpan.FromHours(1));

        #region Execusion Management 

        /// <summary>
        /// attempts to increase executers. 
        /// </summary>
        /// <returns></returns>
        private Task TryIncreaseExecuters()
        {
            
            var currentNumOfExecuters = m_Executers.Count;
            var qNumber = m_QueueManager.Count;


            //we maxed out ?
            // if we add, will it be more than queues?
            if (currentNumOfExecuters == m_MaxNumOfWorkers || (currentNumOfExecuters + 1) > qNumber)            
                return Task.FromResult(0);

            // we are good add
            var newExecuter = new WorkExecuter<Handler, Wi>(this);
            newExecuter.Start();
            m_Executers.AddOrUpdate(newExecuter.m_WorkerExecuterId, newExecuter, (k, v) => { return newExecuter; });


            return Task.FromResult(0);
        }

        private Task TryDecreaseExecuters()
        {


                var currentNumOfExecuters = m_Executers.Count;
                var qNumber = m_QueueManager.Count;
                if (0 == currentNumOfExecuters || currentNumOfExecuters <= qNumber) // not much we can do here
                {
                    Trace.WriteLine(string.Format("a remove request denied E#:{0} Q#:{1}", m_Executers.Count, qNumber));
                    return Task.FromResult(0);
                }

                // first 
                var kvpOtherExecuter = m_Executers.First(kvp => true);
                if (kvpOtherExecuter.Value != null)
                {

                    WorkExecuter<Handler, Wi> OtherExecuter;
                    var bSucess = m_Executers.TryRemove(kvpOtherExecuter.Key, out OtherExecuter);
                    if (bSucess)
                    {
                        OtherExecuter.Stop();
                        OtherExecuter = null;
                    }
                }
                return Task.FromResult(0);
        }

        private Handler GetHandlerForQueue(string qName)
        {
            switch (m_WorkItemHandlerMode)
            {
                case WorkItemHandlerMode.Singlton:
                    {
                        return m_handlers.GetOrAdd("0", (key) => { return new Handler(); });
                    }
                case WorkItemHandlerMode.PerWorkItem:
                    {
                        return new Handler();
                    }
                case WorkItemHandlerMode.PerQueue:
                    {
                        return m_handlers.GetOrAdd(qName, (key) => { return new Handler(); });                    
                    }
            }
            return default(Handler);
        }
        private Task RemoveHandlerForQueue(string qName)
        {
            return Task.Run(() =>
           {
               Handler handler;
               m_handlers.TryRemove(qName, out handler);
           });
        }

        #endregion

        #region Telemetry Management
        private async Task LoadNumOfBufferedItems()
        {
            long buffered = 0;
            

            foreach (var qName in m_QueueManager.QueueNames)
            {
                var q = await m_QueueManager.GetOrAddQueueAsync(qName);
                buffered += await q.GetCountAsync(); 
            }
            m_NumOfBufferedWorkItems = buffered;
        }
        private void IncreaseBufferedWorkItems()
        {
            Interlocked.Increment(ref m_NumOfBufferedWorkItems);
            m_MinuteClicker.Click(new WorkManagerClick() { ClickType = WorkerManagerClickType.Posted });

        }
        private void DecreaseBufferedWorkItems()
        {
            Interlocked.Decrement(ref m_NumOfBufferedWorkItems);
            m_MinuteClicker.Click(new WorkManagerClick() { ClickType = WorkerManagerClickType.Processed });
        }

        private void OnMinuteClickerTrim(WorkManagerClick head)
        {
            // roll up totals into mins 
            var totalAdd = 0;
            var totalProcessed = 0;

            var curr = head;
            while (curr != null)
            {
                if (curr.ClickType == WorkerManagerClickType.Posted)
                    totalAdd++;
                else
                    totalProcessed++;

                curr = (WorkManagerClick) curr.Next;
            }

            // harvest 
            m_HourClicker.Click(new WorkManagerClick() { ClickType = WorkerManagerClickType.Posted, Value = totalAdd });
            m_HourClicker.Click(new WorkManagerClick() { ClickType = WorkerManagerClickType.Processed, Value = totalProcessed});
        }

        #endregion

        #region Specs
        public uint YieldQueueAfter
        {
            get { return m_Yield_Queue_After; }
            set {
                if (0 == value)
                    return;

                    m_Yield_Queue_After = value;
                }
        }
        public uint MaxNumOfWorkers
        {
            get { return m_MaxNumOfWorkers; }
            set
            {
                if (0 == value)
                    return;

                if (value > s_Max_Num_OfWorker)
                    m_MaxNumOfWorkers = s_Max_Num_OfWorker;

                m_MaxNumOfWorkers = value;
            }
        }
        public uint MaxNumOfBufferedWorkItems
        {
            get { return m_MaxNumOfBufferedWorkItems; }
            set
            {
                if (0 == value)
                    return;

                if (value > s_MaxNumOfBufferedWorkItems)
                    m_MaxNumOfBufferedWorkItems = s_MaxNumOfBufferedWorkItems;

                m_MaxNumOfBufferedWorkItems = value;
            }
        }


        public IReliableStateManager StateManager
        {
            get
            {
                return m_StateManager;
            }
        }
        public  WorkItemHandlerMode WorkItemHandlerMode
        {
            get { return m_WorkItemHandlerMode; }
            set
            {
                if (m_WorkManagerStatus == WorkManagerStatus.Working)
                    throw new InvalidOperationException("can not change work item handler mode while working is running");

                if (value != m_WorkItemHandlerMode)
                    m_handlers.Clear();

                m_TraceWriter.TraceMessage(string.Format("Work manager is changing work item handling mode from {0} to {1}", m_WorkItemHandlerMode.ToString(), value.ToString()));
                m_WorkItemHandlerMode = value;
            }
        }
        public WorkManagerStatus WorkManagerStatus
        {
            get { return m_WorkManagerStatus; }
        }
        public TimeSpan RemoveEmptyQueueAfter
        {
            get { return  TimeSpan.FromTicks(m_RemoveEmptyQueueAfterTicks); }
            set {

               
                m_RemoveEmptyQueueAfterTicks = value.Ticks;
            }
        }
        #endregion

        #region runtime Telemetry
        public int NumberOfActiveQueues
        {
            get
            {
                return m_QueueManager.QueueNames.Count();
            }
        }

        public int TotalPostedLastMinute
        {
            get
            {
                return m_MinuteClicker.Do(head =>
                {
                    int count = 0;
                    var curr = head;
                    while (curr != null)
                    {
                        if(curr.ClickType == WorkerManagerClickType.Posted)
                            count++;

                        curr = (WorkManagerClick) curr.Next;
                    }
                    return count;
                 });
            }
        }
        public int TotalProcessedLastMinute
        {
            get
            {

                return m_MinuteClicker.Do(head =>
                {
                    int count = 0;
                    var curr = head;
                    while (curr != null )
                    {
                        if(curr.ClickType == WorkerManagerClickType.Processed)
                            count++;

                        curr = (WorkManagerClick)curr.Next;
                    }
                    return count;
                });

            }
        }
        public int TotalPostedLastHour
        {
            get
            {
                return m_HourClicker.Do(head =>
                {
                    int count = 0;
                    var curr = head;
                    while (curr != null )
                    {
                        if(curr.ClickType == WorkerManagerClickType.Posted)
                            count +=  curr.Value;

                        curr = (WorkManagerClick)curr.Next;
                    }
                    return count;
                });
            }
        }
        public int TotalProcessedLastHour
        {
            get
            {

                return m_HourClicker.Do(head =>
                {
                    int count = 0;
                    var curr = head;
                    while (curr != null )
                    {
                        if(curr.ClickType == WorkerManagerClickType.Processed)
                            count += curr.Value;

                        curr = (WorkManagerClick)curr.Next;
                    }
                    return count;
                });

            }
        }
        public float AveragePostedPerMinLastHour
        {
            get
            {

                return m_HourClicker.Do(head =>
                {
                    int sum = 0;
                    int count = 0;
                    var curr = head;
                    while (curr != null )
                    {
                        if (curr.ClickType == WorkerManagerClickType.Posted)
                        { 
                            sum += (int) curr.Value;
                            count++;
                        }
                        curr = (WorkManagerClick)curr.Next;
                    }
                    return count == 0 ? 0 : sum/count ;
                });
            }

        }
        public float AverageProcessedPerMinLastHour
        {
            get
            {

                return m_HourClicker.Do(head =>
                {
                    int sum = 0;
                    int count = 0;
                    var curr = head;
                    while (curr != null )
                    {
                        if(curr.ClickType == WorkerManagerClickType.Processed)
                        { 
                            sum += (int)curr.Value;
                            count++;
                        }
                        curr = (WorkManagerClick)curr.Next;
                    }
                    return count == 0 ? 0 : sum / count;
                });
            }

        }



        public long NumberOfBufferedWorkItems
        {
            get { return m_NumOfBufferedWorkItems; }
        }
#endregion
        
        #region Control 
        public async Task StartAsync()
        {
            if (m_WorkManagerStatus != WorkManagerStatus.New)
                throw new InvalidOperationException("can not start a non-new work manager");

            m_QueueManager = await QueueManager<Handler, Wi>.CreateAsync(this);
            
            await this.LoadNumOfBufferedItems();

            // create initial executers one per q (keeping in max value). 
            var nTargetExecuters = Math.Min(m_QueueManager.Count, m_MaxNumOfWorkers);
            for (var i = 1; i <= nTargetExecuters; i++)
                m_DeferedTaskExec.AddWork(TryIncreaseExecuters);


            m_WorkManagerStatus = WorkManagerStatus.Working;
        }
        public Task PauseAsync()
        {
            return Task.Run(() => {
                m_WorkManagerStatus = WorkManagerStatus.Paused;
                foreach (var e in m_Executers.Values)
                    e.Pause();
            });
        }
        public Task ResumeAsync()
        {
            return Task.Run(() => {
                m_WorkManagerStatus = WorkManagerStatus.Working;
                foreach (var e in m_Executers.Values)
                    e.Resume();
            });
        }
        public Task DrainAndStopAsync()
        {
            return Task.Run(async () => {

                if (m_WorkManagerStatus == WorkManagerStatus.Paused)
                    await ResumeAsync();

                m_WorkManagerStatus = WorkManagerStatus.Draining;


                while (m_NumOfBufferedWorkItems > 0)
                    await Task.Delay(5 * 1000);

                m_WorkManagerStatus = WorkManagerStatus.Stopped;
            });
        }
        public Task StopAsync()
        {
            return Task.Run(() => {
                m_WorkManagerStatus = WorkManagerStatus.Stopped;
                foreach (var e in m_Executers.Values)
                    e.Stop();

                m_Executers.Clear();
            });
        }

        #endregion

        public async Task PostWorkItemAsync(Wi workItem)
        {
            if (m_WorkManagerStatus != WorkManagerStatus.Working)
                throw new InvalidOperationException("Work Manager is not working state");

            if (m_NumOfBufferedWorkItems >= m_MaxNumOfBufferedWorkItems)
                throw new InvalidOperationException(string.Format("Work Manger is at maximum buffered work items:{0}", m_NumOfBufferedWorkItems));


            try
            {
                // Which Q
                var targetQueue = await m_QueueManager.GetOrAddQueueAsync(workItem.QueueName);

                // enqueue
                using (var tx = m_StateManager.CreateTransaction())
                {
                    await targetQueue.EnqueueAsync(tx, workItem, TimeSpan.FromSeconds(10), CancellationToken.None);
                    await tx.CommitAsync();
                }
                IncreaseBufferedWorkItems();
                m_DeferedTaskExec.AddWork(TryIncreaseExecuters);
            }
            catch (AggregateException aex)
            {
                var ae = aex.Flatten();

                m_TraceWriter.TraceMessage(string.Format("Post to work manager failed, caller should retry E:{0} StackTrace:{1}" , ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace()));
                throw;
            }

        }

        public WorkManager(IReliableStateManager StateManager, ITraceWriter TraceWriter)
        {
            if (null == StateManager)
                throw new ArgumentNullException("StateManager");

            // wire up the minute clicker. 
            m_MinuteClicker.OnTrim = (head) => OnMinuteClickerTrim(head);

            m_StateManager = StateManager;

            m_TraceWriter = TraceWriter;


            m_TraceWriter.TraceMessage("a new work manager was created for this replica");
        }

    }
}
