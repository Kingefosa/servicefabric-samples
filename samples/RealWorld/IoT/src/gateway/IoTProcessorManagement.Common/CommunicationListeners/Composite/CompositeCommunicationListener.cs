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
using System.Collections.Concurrent;

namespace IoTProcessorManagement.Common
{
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
        private ICommunicationListenerStatus m_CompositeListenerStatus = ICommunicationListenerStatus.Closed;
        private ServiceInitializationParameters m_ServiceInitializationParameters;
        private ITraceWriter m_TraceWriter;

        private void EnsureFuncs()
        {
            if (null == OnCreateListeningAddress)
            {
                OnCreateListeningAddress = (listener, addresses) =>
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var address in addresses.Values)
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

            if ((m_listeners.Where(kvp => null == kvp.Value).Count()) > 0)
                throw new InvalidOperationException("can not have null listeners");

        }

        public Func<CompositeCommunicationListener, Dictionary<string,string>, string> OnCreateListeningAddress
        {
            get;
            set;
        }
        public KeyValuePair<string, ICommunicationListener>[] Listners
        {
            get { return m_listeners.ToArray(); }
        }
        public ICommunicationListenerStatus GetListenerStatus(string ListenerName)
        {
            if (!m_Statuses.ContainsKey(ListenerName))
                throw new InvalidOperationException(string.Format("Listener with the name {0} does not exist", ListenerName));

            return m_Statuses[ListenerName];
         }

        public ICommunicationListenerStatus CompsiteListenerStatus
        {
            get { return m_CompositeListenerStatus; }
        }

        public CompositeCommunicationListener(ITraceWriter TraceWriter): this(TraceWriter, null)
        {
        }
        public CompositeCommunicationListener(ITraceWriter TraceWriter, Dictionary<string, ICommunicationListener> listeners)
        {

            m_TraceWriter = TraceWriter;

            if (null != listeners)
                foreach (var kvp in listeners)
                {
                    m_TraceWriter.TraceMessage(string.Format("Composite listener added a new listener name:{0}", kvp.Key));
                    m_listeners.Add(kvp.Key, kvp.Value);
                    m_Statuses.Add(kvp.Key, ICommunicationListenerStatus.Closed);
                }
        }
        public async Task AddListenerAsync(string Name, ICommunicationListener listener)
        {
            
            try
            {
                if (null == listener)
                    throw new ArgumentNullException("listener");

                if (m_listeners.ContainsKey(Name))
                    throw new InvalidOperationException(string.Format("Listener with the name {0} already exists", Name));


                m_listenerLock.WaitOne();

                m_listeners.Add(Name, listener);
                m_Statuses.Add(Name, ICommunicationListenerStatus.Closed);

                m_TraceWriter.TraceMessage(string.Format("Composite listener added a new listener name:{0}", Name));


                if (ICommunicationListenerStatus.Initialized == m_CompositeListenerStatus ||
                   ICommunicationListenerStatus.Opened == m_CompositeListenerStatus)
                { 
                    _InitListener(Name, listener);

                    if(ICommunicationListenerStatus.Opened == m_CompositeListenerStatus)
                        await _OpenListener(Name, listener, CancellationToken.None);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listenerLock.Set();
            }
             
        }
        public async Task RemoveListenerAsync(string Name)
        {
            ICommunicationListener listener = null;

            try
            {
                if (!m_listeners.ContainsKey(Name))
                    throw new InvalidOperationException(string.Format("Listener with the name {0} does not exists", Name));

                listener = m_listeners[Name];


                m_listenerLock.WaitOne();
                await _CloseListener(Name, listener, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (AggregateException aex)
            {
                var ae = aex.Flatten();
                m_TraceWriter.TraceMessage(string.Format("Compsite listen failed to close (for removal) listener:{0} it will be forcefully aborted E:{1} StackTrace:{2}", Name, ae.GetCombinedExceptionMessage(), ae.GetCombinedExceptionStackTrace()));

                // force abkrted
                if (null != listener)
                { 
                    try { listener.Abort(); } catch { /*no op*/}
                }
            }
            finally
            {

                m_listeners.Remove(Name);
                m_Statuses.Remove(Name);

                m_listenerLock.Set();
            }
        }
        public void Abort()
        {
            try
            {
                m_listenerLock.WaitOne();

                m_CompositeListenerStatus = ICommunicationListenerStatus.Aborting;
                foreach (var kvp in m_listeners)
                    _AbortListener(kvp.Key, kvp.Value);

                m_CompositeListenerStatus = ICommunicationListenerStatus.Aborted;

            }
            catch
            {
                throw;
            }
            finally
            {
                m_listenerLock.Set();
            }

            
            
            

        }
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                m_listenerLock.WaitOne();
                m_CompositeListenerStatus = ICommunicationListenerStatus.Closing;

                var tasks = new List<Task>();
                foreach (var kvp in m_listeners)
                    tasks.Add(_CloseListener(kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(tasks);
                m_CompositeListenerStatus = ICommunicationListenerStatus.Closed;
                
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listenerLock.Set();
            }
        }
        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            try
            {
                m_listenerLock.WaitOne();

                m_ServiceInitializationParameters = serviceInitializationParameters;
                foreach (var kvp in m_listeners)
                      _InitListener(kvp.Key, kvp.Value);
            }
            catch
            {
                throw;
            }
            finally
            { 
                m_listenerLock.Set();
            }
        }
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                ValidateListeners();

                m_listenerLock.WaitOne();

                m_CompositeListenerStatus = ICommunicationListenerStatus.Opening;

                var tasks = new List<Task<KeyValuePair<string,string>>>();
                var addresses = new Dictionary<string, string>();

                foreach (var kvp in m_listeners)
                    tasks.Add(
                        Task.Run(
                            async () =>
                                {
                                    var PublishAddress = await _OpenListener(kvp.Key, kvp.Value, cancellationToken);

                                    return new KeyValuePair<string, string>
                                                (
                                                    kvp.Key,
                                                    PublishAddress
                                                );
                                }));

                await Task.WhenAll(tasks);

                foreach (var task in tasks)
                    addresses.Add(task.Result.Key, task.Result.Value);

                EnsureFuncs();
                m_CompositeListenerStatus = ICommunicationListenerStatus.Opened;
                return OnCreateListeningAddress(this, addresses);

            }
            catch
            {
                throw;
            }
            finally
            {
                m_listenerLock.Set();
            }
            
        }


        #region Per Listener

        private void _InitListener(string ListenerName, 
                                    ICommunicationListener listener)
        {
          

            m_Statuses[ListenerName] = ICommunicationListenerStatus.Initializing;
            listener.Initialize(m_ServiceInitializationParameters);
            m_Statuses[ListenerName] = ICommunicationListenerStatus.Initialized;

            m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} initialized", ListenerName));

        }


        private async Task<string> _OpenListener(string ListenerName, 
                                                ICommunicationListener listener, 
                                                CancellationToken canceltoken)
        {




                m_Statuses[ListenerName] = ICommunicationListenerStatus.Opening;
                var sAddress = await listener.OpenAsync(canceltoken);
                m_Statuses[ListenerName] = ICommunicationListenerStatus.Opened;

            m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} opened on {1}", ListenerName, sAddress));

            return sAddress;
        }


        private async Task _CloseListener(string ListenerName, 
                                          ICommunicationListener listener, 
                                          CancellationToken cancelToken)
        {

         
                m_Statuses[ListenerName] = ICommunicationListenerStatus.Closing;
                await listener.CloseAsync(cancelToken);
                m_Statuses[ListenerName] = ICommunicationListenerStatus.Closed;

            m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} closed", ListenerName));
        }

        private void _AbortListener(string ListenerName, 
                                    ICommunicationListener listener)
        {
                m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborting;
                listener.Abort();
                m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborted;

                m_TraceWriter.TraceMessage(string.Format("Composite listener - listener {0} aborted", ListenerName));
        }

        #endregion
    }
}
