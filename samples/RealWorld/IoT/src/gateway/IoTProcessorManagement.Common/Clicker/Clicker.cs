using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    /// an memory histogram implementation, supports 
    /// periodical trim (with Action<Linked List Head> call backs). 
    /// counts and external actions (such as sum, average etc) via Func delegats
    /// </summary>
    /// <typeparam name="T">Click Type</typeparam>
    public class Clicker<T> : IDisposable where T : Click, new()
    {
        private Task m_TrimTask = null;
        private T m_head = new T();
        private CancellationTokenSource m_cts = new CancellationTokenSource();

        private Action<T> m_OnTrim = null;
        private bool m_OnTrimChanged = false;


        private T CloneInTimeSpan(TimeSpan ts)
        {
            long ticksWhen = DateTime.UtcNow.Ticks - ts.Ticks;

            var head = new T();
            var ret = head;
            var cur = m_head;

            while (cur != null && cur.When >= ticksWhen)
            {
                head.Next = (T)cur.Clone();
                head = (T)head.Next;
                cur = (T)cur.Next;
            }
            head.Next = null;
            return (T)ret.Next;
        }
        /// <summary>
        /// Will be called whenever KeepClicksFor elabsed
        /// </summary>
        public Action<T> OnTrim
        {
            get { return m_OnTrim; }
            set
            {
                if (null == value)
                    throw new ArgumentNullException("OnTrim");

                m_OnTrim = value;
                m_OnTrimChanged = true;
            }
        }
        public TimeSpan KeepClicksFor { get; set; }
        private async Task TrimLoop()
        {
            while (!m_cts.IsCancellationRequested)
            {
                await Task.Delay((int)KeepClicksFor.TotalMilliseconds);
                Trim();
            }
        }
        public Clicker(TimeSpan KeepFor)
        {
            m_head.When = 0; // this ensure that head is never counted. 
            KeepClicksFor = KeepFor;
            m_TrimTask = Task.Run(async () => await TrimLoop());
        }
        public Clicker() : this(TimeSpan.FromMinutes(1))
        {

        }
        public void Click(T newNode)
        {
            // set new head. 
            do
            {
                newNode.Next = m_head;
            }
            while (newNode.Next != Interlocked.CompareExchange<T>(ref m_head, newNode, (T)newNode.Next));
        }
        public void Click()
        {
            T node = new T();
            Click(node);
        }

        public M Do<M>(Func<T, M> func)
        {
            return Do(KeepClicksFor, func);
        }

        public M Do<M>(TimeSpan ts, Func<T, M> func)
        {
            if (ts > KeepClicksFor)
                throw new ArgumentException("Can not do for a timespan more than what clicker is keeping track of");


            // since we are not sure what will happen in
            // the func, we are handing out a copy not the original thing
            return func(CloneInTimeSpan(ts));
        }
        public int Count()
        {
            return Count(KeepClicksFor);
        }
        public int Count(TimeSpan ts)
        {
            if (ts > KeepClicksFor)
                throw new ArgumentException("Can not count for a timespan more than what clicker is keeping track of");

            long ticksWhen = DateTime.UtcNow.Ticks - ts.Ticks;
            int count = 0;
            Click cur = m_head;

            while (null != cur && cur.When >= ticksWhen)
            {
                count++;
                cur = cur.Next;
            }
            return count;
        }
        private void Trim()
        {
            // trim keeps the head. 
            long ticksWhen = DateTime.UtcNow.Ticks - KeepClicksFor.Ticks;
            Click cur = m_head;
            Click next = cur.Next;
            while (null != next)
            {
                if (next.When <= ticksWhen)
                {
                    cur.Next = null;
                    break;
                }
                cur = next;
                next = next.Next;
            }

            // call on trim
            if (m_OnTrimChanged)
                OnTrim(CloneInTimeSpan(KeepClicksFor));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_cts.Cancel();
                }
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
