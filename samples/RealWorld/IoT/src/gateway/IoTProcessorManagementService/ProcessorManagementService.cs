// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IoTProcessorManagementService
{
    using System;
    using System.Fabric.Description;
    using System.Threading;
    using System.Threading.Tasks;
    using IoTProcessorManagement.Clients;
    using IoTProcessorManagement.Common;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;

    public class ProcessorManagementService : StatefulService
    {
        public static readonly string s_OperationQueueName = "Opeartions";
        public static readonly string s_ProcessorDefinitionStateDictionaryName = "Processors";
        public static readonly int s_MaxProcessorOpeartionRetry = 5;


        public ProcessorManagementService()
        {
        }


        public IReliableDictionary<string, Processor> ProcessorStateStore { get; private set; }

        public IReliableQueue<ProcessorOperation> ProcessorOperationsQueue { get; private set; }


        public ProcessorManagementServiceConfig Config { get; private set; }

        public ProcessorOperationHandlerFactory m_ProcessorOperationFactory { get; private set; }

        public ProcessorServiceCommunicationClientFactory m_ProcessorServiceCommunicationClientFactory { get; private set; }
            = new ProcessorServiceCommunicationClientFactory(
                ServicePartitionResolver.GetDefault(),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(3));

        protected override ICommunicationListener CreateCommunicationListener()
        {
            // create a new Owin listener that uses our spec
            // which needs state manager (to be injected in relevant controllers). 
            ProcessorManagementServiceOwinListenerSpec spec = new ProcessorManagementServiceOwinListenerSpec();
            spec.Svc = this;
            return new OwinCommunicationListener(spec);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            this.SetProcessorAppInstanceDefaults();

            // subscribe to configuration changes
            this.ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            this.ProcessorStateStore = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Processor>>(s_ProcessorDefinitionStateDictionaryName);
            this.ProcessorOperationsQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<ProcessorOperation>>(s_OperationQueueName);

            this.m_ProcessorOperationFactory = new ProcessorOperationHandlerFactory();

            ProcessorOperation wo = null;
            // pump and execute ProcessorPperation from the queue
            while (!cancellationToken.IsCancellationRequested)
            {
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    try
                    {
                        ConditionalResult<ProcessorOperation> result = await this.ProcessorOperationsQueue.TryDequeueAsync(
                            tx,
                            TimeSpan.FromMilliseconds(1000),
                            cancellationToken);
                        if (result.HasValue)
                        {
                            wo = result.Value;
                            ProcessorOperationHandlerBase handler = this.m_ProcessorOperationFactory.CreateHandler(this, wo);
                            await handler.RunOperation(tx);
                            await tx.CommitAsync();
                        }
                    }
                    catch (TimeoutException toe)
                    {
                        ServiceEventSource.Current.Message(
                            string.Format("Controller service encountered timeout in a work operations de-queue process {0} and will try again", toe.StackTrace));
                    }
                    catch (AggregateException aex)
                    {
                        AggregateException ae = aex.Flatten();

                        string sError = string.Empty;
                        if (null == wo)
                            sError =
                                string.Format(
                                    "Event Processor Management Service encountered an error processing Processor-Operation {0} {1} and will terminate replica",
                                    ae.GetCombinedExceptionMessage(),
                                    ae.GetCombinedExceptionStackTrace());
                        else
                            sError =
                                string.Format(
                                    "Event Processor Management Service encountered an error processing Processor-opeartion {0} against {1} Error {2} stack trace {3} and will terminate replica",
                                    wo.OperationType.ToString(),
                                    wo.ProcessorName,
                                    ae.GetCombinedExceptionMessage(),
                                    ae.GetCombinedExceptionStackTrace());


                        ServiceEventSource.Current.ServiceMessage(this, sError);
                        throw;
                    }
                }
            }
        }

        #region Configuration Management 

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(
            object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            this.SetProcessorAppInstanceDefaults();
        }

        private void SetProcessorAppInstanceDefaults()
        {
            /// <summary>
            /// loads default processor app type default name and version
            /// from and configuration and saves them for later use 

            ConfigurationSettings settingsFile =
                this.ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
            ConfigurationSection ProcessorServiceDefaults = settingsFile.Sections["ProcessorDefaults"];

            ProcessorManagementServiceConfig newConfig = new ProcessorManagementServiceConfig
                (
                ProcessorServiceDefaults.Parameters["AppTypeName"].Value,
                ProcessorServiceDefaults.Parameters["AppTypeVersion"].Value,
                ProcessorServiceDefaults.Parameters["ServiceTypeName"].Value,
                ProcessorServiceDefaults.Parameters["AppInstanceNamePrefix"].Value
                );

            this.Config = newConfig;
        }

        #endregion
    }
}