using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Threading;

using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using System.Globalization;

namespace IoTGateway.Common
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        private IDisposable m_WebServer = null;
        private string m_ListeningAddress = string.Empty;
        private string m_PublishingAddress = string.Empty;
        private ServiceInitializationParameters m_ServiceInitializationParameters;


        public Func<OwinCommunicationListener,string> OnCreateListeningAddress { get; set; }
        public Func<OwinCommunicationListener, string> OnCreatePublishingAddress { get; set; }
        public IOwinListenerSpec PipelineSpec { get; set; }
        public ServiceInitializationParameters InitializationParameters
        {
            get { return m_ServiceInitializationParameters; }
        }

        private void EnsureFuncs()
        {
            if (null == PipelineSpec)
                throw new InvalidOperationException("Owin pipeline specification is null");

            // in case of no function pointers set to create listening and publishing address
            // we use the default below. 

            if (null == OnCreateListeningAddress)
                OnCreateListeningAddress = (listener) =>
                {
                    StatefulServiceInitializationParameters statefulInitParam;

                    var bIsStateful = (null != (statefulInitParam = listener.InitializationParameters as StatefulServiceInitializationParameters));
                    var port = listener.InitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndPoint").Port;


                    if (bIsStateful)
                        return String.Format(
                                    CultureInfo.InvariantCulture,
                                    "http://{0}:{1}/{2}/{3}/",
                                    FabricRuntime.GetNodeContext().IPAddressOrFQDN,
                                    port,
                                    statefulInitParam.PartitionId,
                                    statefulInitParam.ReplicaId);
                    else
                        return String.Format(
                                    CultureInfo.InvariantCulture,
                                    "http://{0}:{1}/",
                                    FabricRuntime.GetNodeContext().IPAddressOrFQDN,
                                    port);
                };


            if (null == OnCreatePublishingAddress)
                OnCreatePublishingAddress = (listener) =>
                {
                    return listener.m_ListeningAddress;
                };
        }

        public OwinCommunicationListener()
        {

        }

        public OwinCommunicationListener(IOwinListenerSpec pipelineSpec)
        {
            this.PipelineSpec = pipelineSpec;
        }
        public void Abort()
        {
            if (null != m_WebServer)
                m_WebServer.Dispose();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                Abort();
            });
        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            this.m_ServiceInitializationParameters = serviceInitializationParameters;

            EnsureFuncs();

            m_ListeningAddress = OnCreateListeningAddress( this);
            m_PublishingAddress = OnCreatePublishingAddress(this);
        }
        
        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {

          
            return Task.Run(() =>
                    {  
                        m_WebServer =  WebApp.Start(this.m_ListeningAddress, app => PipelineSpec.CreateOwinPipeline(app));
                        return m_PublishingAddress;
                    });
          
        }
    }
}
