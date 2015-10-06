using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;
using System.Collections.Concurrent;

namespace IoTGateway.Common
{
    public class CompositeCommunicationListener : ICommunicationListener
    {
        private Dictionary<string, ICommunicationListener> m_listeners = new Dictionary<string, ICommunicationListener>();
        private Dictionary<string, ICommunicationListenerStatus> m_Statuses = new Dictionary<string, ICommunicationListenerStatus>();


        private AutoResetEvent m_listnerLock = new AutoResetEvent(true);
        private ICommunicationListenerStatus m_ListenerStatus = ICommunicationListenerStatus.Closed;
        private ServiceInitializationParameters m_ServiceInitializationParameters;
   

        private void EnsureFuncs()
        {
            if (null == OnCreateListeningAddress)
            {
                OnCreateListeningAddress = (listener, addresses) =>
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var address in addresses)
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

        public Func<CompositeCommunicationListener, List<string>, string> OnCreateListeningAddress
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


        public CompositeCommunicationListener(): this(null)
        {
        }
        public CompositeCommunicationListener(Dictionary<string, ICommunicationListener> listeners)
        {

            if (null != listeners)
                foreach (var kvp in listeners)
                { 
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


                m_listnerLock.WaitOne();
                m_listeners.Add(Name, listener);
                m_Statuses.Add(Name, ICommunicationListenerStatus.Closed);

                _InitListner(Name, listener);
                await _OpenListener(Name, listener, CancellationToken.None);
                
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listnerLock.Set();
            }
             
        }
        public async Task RemoveListenerAsync(string Name)
        {
            try
            {
                if (m_listeners.ContainsKey(Name))
                    throw new InvalidOperationException(string.Format("Listener with the name {0} does not exists", Name));


                m_listnerLock.WaitOne();
                var listener = m_listeners[Name];
                await _CloseListener(Name, listener, CancellationToken.None);
                m_listeners.Remove(Name);
                m_Statuses.Remove(Name);
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listnerLock.Set();
            }
        }
        public void Abort()
        {
            try
            {
                m_listnerLock.WaitOne();

                m_ListenerStatus = ICommunicationListenerStatus.Aborting;
                foreach (var kvp in m_listeners)
                    _AbortListener(kvp.Key, kvp.Value);

                m_ListenerStatus = ICommunicationListenerStatus.Aborted;

            }
            catch
            {
                throw;
            }
            finally
            {
                m_listnerLock.Set();
            }

            
            
            

        }
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                m_ListenerStatus = ICommunicationListenerStatus.Closing;
                m_listnerLock.WaitOne();

                var tasks = new List<Task>();
                foreach (var kvp in m_listeners)
                    tasks.Add(_CloseListener(kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(tasks);
                m_ListenerStatus = ICommunicationListenerStatus.Closed;
                
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listnerLock.Set();
            }
        }
        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            try
            {
                m_listnerLock.WaitOne();

                m_ServiceInitializationParameters = serviceInitializationParameters;
                foreach (var kvp in m_listeners)
                      _InitListner(kvp.Key, kvp.Value);
            }
            catch
            {
                throw;
            }
            finally
            { 
                m_listnerLock.Set();
            }
        }
        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                ValidateListeners();

                m_listnerLock.WaitOne();


                var tasks = new List<Task<string>>();
                var addresses = new List<string>();

                foreach (var kvp in m_listeners)
                    tasks.Add(_OpenListener(kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(tasks);

                foreach (var task in tasks)
                    addresses.Add(task.Result);

                EnsureFuncs();

                return OnCreateListeningAddress(this, addresses);
            }
            catch
            {
                throw;
            }
            finally
            {
                m_listnerLock.Set();
            }
            
        }




        private void _InitListner(string ListenerName, 
                                    ICommunicationListener listener)
        {
            if (m_Statuses[ListenerName] == ICommunicationListenerStatus.Initializing ||
               m_Statuses[ListenerName] == ICommunicationListenerStatus.Initialized)
                return ;

            m_Statuses[ListenerName] = ICommunicationListenerStatus.Initializing;
            listener.Initialize(m_ServiceInitializationParameters);
            m_Statuses[ListenerName] = ICommunicationListenerStatus.Initialized;
        }


        private async Task<string> _OpenListener(string ListenerName, 
                                                ICommunicationListener listener, 
                                                CancellationToken canceltoken)
        {
            if (m_Statuses[ListenerName] == ICommunicationListenerStatus.Opening ||
                m_Statuses[ListenerName] == ICommunicationListenerStatus.Opened)
                return "";


            m_Statuses[ListenerName] = ICommunicationListenerStatus.Opening;
            var sAddress = await listener.OpenAsync(canceltoken);
            m_Statuses[ListenerName] = ICommunicationListenerStatus.Opened;

            return sAddress;
        }


        private async Task _CloseListener(string ListenerName, 
                                          ICommunicationListener listener, 
                                          CancellationToken cancelToken)
        {

            if (m_Statuses[ListenerName] == ICommunicationListenerStatus.Closing||
                m_Statuses[ListenerName] == ICommunicationListenerStatus.Closed)
                return;


            m_Statuses[ListenerName] = ICommunicationListenerStatus.Closing;
            await listener.CloseAsync(cancelToken);
            m_Statuses[ListenerName] = ICommunicationListenerStatus.Closed;
        }

        private void _AbortListener(string ListenerName, 
                                    ICommunicationListener listener)
        {
            if (m_Statuses[ListenerName] == ICommunicationListenerStatus.Aborted ||
                m_Statuses[ListenerName] == ICommunicationListenerStatus.Aborted)
                return;

            m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborting;
            listener.Abort();
            m_Statuses[ListenerName] = ICommunicationListenerStatus.Aborted;
        }
    }
}
