using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTGateway.Common
{

    public partial class WorkManager<Handler, Wi> where Handler : IWorkItemHandler<Wi>, new()
                                              where Wi : IWorkItem
    {
        /// <summary>
        /// this class maintains (presists) a list of queues it also
        /// present them as a queue (qOfq). this ensure 1:1 worker to q
        /// to maintain minimum (& fast) presistsance, queue names (not the queue of queue) is presisted.
        /// if the worker process crashed, the queue of queue is removed and the queue of queue will start over 
        /// </summary>
        /// <typeparam name="H"></typeparam>
        /// <typeparam name="W"></typeparam>
         private class QueueManager<H, W> where H : IWorkItemHandler<W>, new()
                                    where W : IWorkItem
        {
            private WorkManager<H, W> m_WorkManager;
            private ConcurrentQueue<string> qOfq = new ConcurrentQueue<string>();
            private ConcurrentDictionary<string, IReliableQueue<W>> m_QueueRefs = new ConcurrentDictionary<string, IReliableQueue<W>>();
            private IReliableDictionary<string, string> m_dictListOfQueues;

            public int Count
            {
                    get { return m_QueueRefs.Keys.Count; }
            }

            public string[] QueueNames
            {
                get { return m_QueueRefs.Keys.ToArray(); }
            }
    
            public async Task<IReliableQueue<W>> GetOrAddQueueAsync(string qName)
            {
                using (var tx = m_WorkManager.StateManager.CreateTransaction())
                {
                    if (!await m_dictListOfQueues.ContainsKeyAsync(tx, qName,TimeSpan.FromSeconds(5), CancellationToken.None))
                    {
                        var reliableQ = await m_WorkManager.StateManager.GetOrAddAsync<IReliableQueue<W>>(tx, qName);
                        await m_dictListOfQueues.AddAsync(tx, qName, qName, TimeSpan.FromSeconds(5), CancellationToken.None);
                        m_QueueRefs.TryAdd(qName, reliableQ);
                        qOfq.Enqueue(qName);
                        await tx.CommitAsync();
                    }
                }




                return m_QueueRefs[qName];
                                
            }
            public async Task<IReliableQueue<W>> GetOrAddQueueAsync(W wi)
            {
                return await GetOrAddQueueAsync(wi.QueueName);
            }

            public async Task RemoveQueueAsync(string qName)
            {
                IReliableQueue<W> q;

                using (var tx = m_WorkManager.StateManager.CreateTransaction())
                {
                    if (await m_dictListOfQueues.ContainsKeyAsync(tx, qName, TimeSpan.FromSeconds(5), CancellationToken.None))
                    {
                        // the queue is left in the queue of queues. the TakeQueueAsync validates if the queue still exist
                        m_QueueRefs.TryRemove(qName, out q);
                        await m_dictListOfQueues.TryRemoveAsync(tx, qName);
                        
                        await m_WorkManager.StateManager.RemoveAsync(tx,qName, TimeSpan.FromSeconds(5));
                        await tx.CommitAsync();
                    }
                }
            }
          
            public  KeyValuePair<string, IReliableQueue<W>> TakeQueueAsync()
            {
                if (0 == qOfq.Count)
                    return new KeyValuePair<string, IReliableQueue<W>>();


                string qName;
                var bSuccess = qOfq.TryDequeue(out qName);
                if (bSuccess && m_QueueRefs.ContainsKey(qName))
                    return new KeyValuePair<string, IReliableQueue<W>>(qName, m_QueueRefs[qName]);
                    

               // queue was previously removed
                return new KeyValuePair<string, IReliableQueue<W>>();
            }

            
            public void LeaveQueueAsync(string sQueueName)
            {
                qOfq.Enqueue(sQueueName);
            }


            #region CTOR/Factory
            private QueueManager(WorkManager<H, W> workManager)
            {
                m_WorkManager = workManager;
            }
            
            public static async Task<QueueManager<H, W>> CreateAsync(WorkManager<H, W> workManager)
            {
                
                // try to create from saved state 
                var dictQueueNames = await workManager.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(WorkManager<H, W>.s_Queue_Names_Dictionary);

                QueueManager<H, W> qManager = new QueueManager<H, W>(workManager);
                qManager.m_dictListOfQueues = dictQueueNames;

                
                foreach (var kvp in dictQueueNames)
                {
                    // preload all refs
                    qManager.m_QueueRefs.TryAdd(kvp.Key, await workManager.StateManager.GetOrAddAsync<IReliableQueue<W>>(kvp.Key)); 
                    qManager.qOfq.Enqueue(kvp.Key);
                }


                return qManager;
            }
            #endregion
        }
    }
}
