// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IoTProcessorManagement.Common
{
    using System;
    using System.Fabric;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin.Hosting;
    using Microsoft.ServiceFabric.Services;

    // generic Owin Listener
    public class OwinCommunicationListener : ICommunicationListener
    {
        private IDisposable m_WebServer = null;
        private string m_ListeningAddress = string.Empty;
        private string m_PublishingAddress = string.Empty;

        public OwinCommunicationListener()
        {
        }

        public OwinCommunicationListener(IOwinListenerSpec pipelineSpec)
        {
            this.PipelineSpec = pipelineSpec;
        }


        public Func<OwinCommunicationListener, string> OnCreateListeningAddress { get; set; }

        public Func<OwinCommunicationListener, string> OnCreatePublishingAddress { get; set; }

        public IOwinListenerSpec PipelineSpec { get; set; }

        public ServiceInitializationParameters InitializationParameters { get; private set; }

        public void Abort()
        {
            if (null != this.m_WebServer)
                this.m_WebServer.Dispose();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => { this.Abort(); });
        }

        public void Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            this.InitializationParameters = serviceInitializationParameters;

            this.EnsureFuncs();

            this.m_ListeningAddress = this.OnCreateListeningAddress(this);
            this.m_PublishingAddress = this.OnCreatePublishingAddress(this);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                () =>
                {
                    this.m_WebServer = WebApp.Start(this.m_ListeningAddress, app => this.PipelineSpec.CreateOwinPipeline(app));
                    return this.m_PublishingAddress;
                });
        }

        private void EnsureFuncs()
        {
            if (null == this.PipelineSpec)
                throw new InvalidOperationException("Owin pipeline specification is null");

            // in case of no function pointers set to create listening and publishing address
            // we use the default below. 

            if (null == this.OnCreateListeningAddress)
                this.OnCreateListeningAddress = (listener) =>
                {
                    StatefulServiceInitializationParameters statefulInitParam;

                    bool bIsStateful = (null != (statefulInitParam = listener.InitializationParameters as StatefulServiceInitializationParameters));
                    int port = listener.InitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndPoint").Port;


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


            if (null == this.OnCreatePublishingAddress)
                this.OnCreatePublishingAddress = (listener) => { return listener.m_ListeningAddress; };
        }
    }
}