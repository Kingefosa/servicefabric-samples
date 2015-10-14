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
    public class DeferredTaskExecuter
    {
        private ConcurrentQueue<Func<Task>> m_Tasks = new ConcurrentQueue<Func<Task>>();
        private Task m_ExecutionTask;
        private CancellationTokenSource m_TokenSource = new CancellationTokenSource();
        private bool m_bAcceptTasks = true;

        public Action<AggregateException> OnError = (ae) => 
        {
            ae.Flatten();
            Trace.WriteLine(string.Format("Deferred task executer encountered error:{0} stacktrace:{1}", ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace()));
        };
         
        public uint NoTaskDelayMs { get; set; }
        public DeferredTaskExecuter()
        {
            NoTaskDelayMs = 1000;
            m_ExecutionTask = Work();
        }

        public bool AddWork(Func<Task> func)
        {
            if(!m_bAcceptTasks)
                return m_bAcceptTasks;

                m_Tasks.Enqueue(func);


            return true;
        }

        public void Stop()
        {
            m_TokenSource.Cancel();
        }

        public void DrainStop()
        {
            m_bAcceptTasks = false;

        }

        public void Restart()
        {
            m_bAcceptTasks = true;
            m_TokenSource = new CancellationTokenSource();
        }

        private async Task Work()
        {
            while (!m_TokenSource.IsCancellationRequested)
            {
                try
                {
                    Func<Task> func;
                    var bfound = m_Tasks.TryDequeue(out func);
                    if (!bfound)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        await func();
                    }

                    if (!m_bAcceptTasks)
                        m_TokenSource.Cancel();

                }
                catch (AggregateException ae)
                {
                    OnError(ae);
                }
            }
        }
    }
}
