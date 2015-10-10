using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;

using IoTProcessorManagement.Common;
using IoTProcessorManagement.Clients;
using System.Diagnostics;

namespace IoTProcessorManagementService
{
   
    public class ProcessorManagementService : StatefulService
    {
        public static readonly string s_OperationQueueName = "Opeartions";
        public static readonly string s_ProcessorDefinitionStateDictionaryName = "Processors";
        public static readonly int s_MaxProcessorOpeartionRetry = 5;


        public IReliableDictionary<string, Processor> ProcessorStateStore { get; private set; }
        public IReliableQueue<ProcessorOperation> ProcessorOperationsQueue { get; private set; }


        public ProcessorAppInstanceDefaults DefaultProcessorAppInstanceDefinition { get; private set; }
        public ProcessorOperationHandlerFactory m_ProcessorOperationFactory { get; private set; }
        public ProcessorServiceCommunicationClientFactory m_ProcessorServiceCommunicationClientFactory { get; private set; } 
            = new ProcessorServiceCommunicationClientFactory(ServicePartitionResolver.GetDefault(),
                                                            TimeSpan.FromSeconds(10),
                                                            TimeSpan.FromSeconds(3));



        public ProcessorManagementService()
        {
        }

#region Configuration Management 
        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            SetProcessorAppInstanceDefaults();
        }

        private void SetProcessorAppInstanceDefaults()
        {

            /// <summary>
            /// loads default processor app type default name and version
            /// from and configuration and saves them for later use 
           
            var settingsFile = ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
            var defaultsSection = settingsFile.Sections["ProcessorDefaults"];

            var processorAppInstanceDefaults = new ProcessorAppInstanceDefaults
                                                (
                                                 defaultsSection.Parameters["AppTypeName"].Value,
                                                 defaultsSection.Parameters["AppTypeVersion"].Value,
                                                 defaultsSection.Parameters["ServiceTypeName"].Value,
                                                 defaultsSection.Parameters["AppInstanceNamePrefix"].Value
                                                );

            DefaultProcessorAppInstanceDefinition = processorAppInstanceDefaults;
        }

#endregion
        protected override ICommunicationListener CreateCommunicationListener()
        {
            // create a new Owin listener that uses our spec
            // which needs state manager (to be injected in relevant controllers). 
            var spec = new ProcessorManagementServiceOwinListenerSpec();
            spec.Svc = this;
            return new OwinCommunicationListener(spec);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            SetProcessorAppInstanceDefaults();

            // subscribe to configuration changes
            base.ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageModifiedEvent += CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            ProcessorStateStore = await StateManager.GetOrAddAsync<IReliableDictionary<string, Processor>>(ProcessorManagementService.s_ProcessorDefinitionStateDictionaryName);
            ProcessorOperationsQueue = await StateManager.GetOrAddAsync<IReliableQueue<ProcessorOperation>>(ProcessorManagementService.s_OperationQueueName);

            m_ProcessorOperationFactory = new ProcessorOperationHandlerFactory();

            ProcessorOperation wo = null;
            // pump and execute ProcessorPperation from the queue
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {

                    try
                    {
                        var result = await ProcessorOperationsQueue.TryDequeueAsync(tx,
                                                                 TimeSpan.FromMilliseconds(100),
                                                                 cancellationToken);
                        if (result.HasValue)
                        {
                            wo = result.Value;
                            var handler = m_ProcessorOperationFactory.CreateHandler(this, wo);
                            await handler.RunOperation(tx);
                            await tx.CommitAsync();
                        }
                    }
                    catch (TimeoutException toe)
                    {
                        ServiceEventSource.Current.Message(string.Format("Controller service encountered timeout in a work operations de-queue process {0} and will try again", toe.StackTrace));
                    }
                    catch (Exception E)
                    {
                        string sError = string.Empty;
                        if (null == wo)
                            sError = string.Format("controller service encountered an error processing Processor-Operation {0} {1} and will terminate replica", E.Message, E.StackTrace);
                        else
                            sError = string.Format("controller service encountered an error processing Processor-opeartion {0} against {1} Error {2} stack trace {3} and will terminate replica",
                                    wo.OperationType.ToString(), wo.ProcessorName, E.Message, E.StackTrace);


                        ServiceEventSource.Current.Message(sError);
                        throw ;
                    }
                   
                }
             }
           }


      }
}
