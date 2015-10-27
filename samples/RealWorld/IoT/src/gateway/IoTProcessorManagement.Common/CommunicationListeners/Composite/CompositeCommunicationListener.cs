// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IoTProcessorManagement.Common
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services;

    /// <summary>
    /// a composite listener is an implementation of ICommunicationListener
    /// surfaced to Service Fabric as one listener but can be a # of 
    /// listeners grouped together. Supports adding listeners even after OpenAsync()
    /// has been called for the listener
    /// </summary>
    public class CompositeCommunicationListener : ICommunicationListener
    {
        private Dictionary<string, ICommunicationListener> m_listeners = new Dictionary<string, ICommunicationListener>();
        private Dictionary<string, ICommunicationListenerStatus> m_Statuses = new Dictionary<string, ICommunicationListenerStatus>();


        private AutoResetEvent m_listenerLock = new AutoResetEvent(true);
        private ServiceInitializationParameters m_ServiceInitializationParameters;
        private ITraceWriter m_TraceWriter;

        public CompositeCommunicationListener(ITraceWriter TraceWriter) : this(TraceWriter, null)
        {
        }

        public CompositeCommunicationListener(ITraceWriter TraceWriter, Dictionary<string, ICommunicationListener> listeners)
        {
            this.m_TraceWriter = TraceWriter;

            if (null != listeners)
                foreach (KeyValuePair<string, ICommunicationListener> kvp in listeners)
                {
                    this.m_TraceWriter.TraceMessage(string.Format("Composite listener added a new listener name:{0}", kvp.Key));
                    this.m_listeners.Add(kvp.Key, kvp.Value);
                    this.m_Statuses.Add(kvp.Key, ICommunicationListenerStatus.Closed);
                }
        }

        public Func<CompositeCommunicationListener, Dictionary<string, string>, string> OnCreateListeningAddress { get; set; }

        public KeyValuePair<string, ICommunicationListener>[] Listners
        {
            get { return this.m_listeners.ToArray(); }
        }

        public ICommunicationListenerStatus CompsiteListenerStatus { get; private set; } = ICommunicationListenerStatus.Closed;

        public void Abort()
        {
            try
            {
                this.m_listenerLock.WaitOne();

                this.CompsiteListenerStatus = ICommunicationListenerStatus.Aborting;
                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.m_listeners)
                    this._AbortListener(kvp.Key, kvp.Value);

                this.CompsiteListenerStatus = ICommunicationListenerStatus.Aborted;
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_listenerLock.Set();
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.m_listenerLock.WaitOne();
                this.CompsiteListenerStatus = ICommunicationListenerStatus.Closing;

                List<Task> tasks = new List<Task>();
                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.m_listeners)
                    tasks.Add(this._CloseListener(kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(tasks);
                this.CompsiteListenerStatus = ICommunicationListenerStatus.Closed;
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_listenerLock.Set();
            }
        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            try
            {
                this.m_listenerLock.WaitOne();

                this.m_ServiceInitializationParameters = serviceInitializationParameters;
                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.m_listeners)
                    this._InitListener(kvp.Key, kvp.Value);
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_listenerLock.Set();
            }
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.ValidateListeners();

                this.m_listenerLock.WaitOne();

                this.CompsiteListenerStatus = ICommunicationListenerStatus.Opening;

                List<Task<KeyValuePair<string, string>>> tasks = new List<Task<KeyValuePair<string, string>>>();
                Dictionary<string, string> addresses = new Dictionary<string, string>();

                foreach (KeyValuePair<string, ICommunicationListener> kvp in this.m_listeners)
                    tasks.Add(
                        Task.Run(
                            async () =>
                            {
                                string PublishAddress = await this._OpenListener(kvp.Key, kvp.Value, cancellationToken);

                                return new KeyValuePair<string, string>
                                    (
                                    kvp.Key,
                                    PublishAddress
                                    );
                            }));

                await Task.WhenAll(tasks);

                foreach (Task<KeyValuePair<string, string>> task in tasks)
                    addresses.Add(task.Result.Key, task.Result.Value);

                this.EnsureFuncs();
                this.CompsiteListenerStatus = ICommunicationListenerStatus.Opened;
                return this.OnCreateListeningAddress(this, addresses);
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_listenerLock.Set();
            }
        }

        public ICommunicationListenerStatus GetListenerStatus(string ListenerName)
        {
            if (!this.m_Statuses.ContainsKey(ListenerName))
                throw new InvalidOperationException(string.Format("Listener with the name {0} does not exist", ListenerName));

            return this.m_Statuses[ListenerName];
        }

        public async Task AddListenerAsync(string Name, ICommunicationListener listener)
        {
            try
            {
                if (null == listener)
                    throw new ArgumentNullException("listener");

                if (this.m_listeners.ContainsKey(Name))
                    throw new InvalidOperationException(string.Format("Listener with the name {0} already exists", Name));


                this.m_listenerLock.WaitOne();

                this.m_listeners.Add(Name, listener);
                this.m_Statuses.Add(Name, ICommunicationListenerStatus.Closed);

                this.m_TraceWriter.TraceMessage(string.Format("Composite listener added a new listener name:{0}", Name));


                if (ICommunicationListenerStatus.Initialized == this.CompsiteListenerStatus ||
                    ICommunicationListenerStatus.Opened == this.CompsiteListenerStatus)
                {
                    this._InitListener(Name, listener);

                    if (ICommunicationListenerStatus.Opened == this.CompsiteListenerStatus)
                        await this._OpenListener(Name, listener, CancellationToken.None);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                this.m_listenerLock.Set();
            }
        }

        public async Task RemoveListenerAsync(string Name)
        {
            ICommunicationListener listener = null;

            try
            {
                if (!this.m_listeners.ContainsKey(Name))
                    throw new InvalidOperationException(string.Format("Listener with the name {0} does not exists", Name));

                listener = this.m_listeners[Name];


                this.m_listenerLock.WaitOne();
                await this._CloseListener(Name, listener, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (AggregateException aex)
            {
                AggregateException ae = aex.Flatten();
                this.m_TraceWriter.TraceMessage(
                    string.Format(
                        "Compsite listen failed to close (for removal) listener:{0} it will be forcefully aborted E:{1} StackTrace:{2}",
                        Name,
                        ae.GetCombinedExceptionMessage(),
                        ae.GetCombinedExceptionStackTrace()));

                // force abkrted
                if (null != listener)
                {
                    try
                    {
                        listener.Abort();
                    }
                    catch
                    {
                        /*no op*/
                    }
                }
            }
            finally
            {
                this.m_listeners.Remove(Name);
                this.m_Statuses.Remove(Name);

                this.m_listenerLock.Set();
            }
        }

        private void EnsureFuncs()
        {
            if (null == this.OnCreateListeningAddress)
            {
                this.OnCreateListeningAddress = (listener, addresses) =>
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string address in addresses.Values)
                        sb.Append(string.Concat(address, ";"));

                    return sb.ToString();
                };
            }
        }

        private void ValidateListeners()
        {
            /*
               services that starts with 0 listners and dynamically add them 
                will have a problem with this

            if (0 == m_listeners.Count)
                throw new InvalidOperationException("can not work with zero listeners");

              */

            if ((this.m_listeners.Where(kvp => null == kvp.Value).Count()) > 0)
                throw new InvalidOperationException("can not have null listeners");
        }

        #region Per Listener

        private void _InitListener(
            string ListenerName,
            ICommunicationListener listener)
        {
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Initializing;
            listener.Initialize(this.m_ServiceInitializationParameters);
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Initialized;

            this.m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} initialized", ListenerName));
        }


        private async Task<string> _OpenListener(
            string ListenerName,
            ICommunicationListener listener,
            CancellationToken canceltoken)
        {
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Opening;
            string sAddress = await listener.OpenAsync(canceltoken);
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Opened;

            this.m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} opened on {1}", ListenerName, sAddress));

            return sAddress;
        }


        private async Task _CloseListener(
            string ListenerName,
            ICommunicationListener listener,
            CancellationToken cancelToken)
        {
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Closing;
            await listener.CloseAsync(cancelToken);
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Closed;

            this.m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} closed", ListenerName));
        }

        private void _AbortListener(
            string ListenerName,
            ICommunicationListener listener)
        {
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborting;
            listener.Abort();
            this.m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborted;

            this.m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} aborted", ListenerName));
        }

        #endregion
    }
}