using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;

using IoTGateway.Common;
using IoTGateway.Clients;
using System.Diagnostics;

namespace CtrlSvc
{
    public class WorkerAppDefinition
    {
        public WorkerAppDefinition(string workerAppTypeName, 
                                   string workerAppTypeVersion, 
                                   string serviceTypeName,
                                   string workerAppNamePrefix)
        {
            AppTypeName = workerAppTypeName;
            AppTypeVersion = workerAppTypeVersion;
            ServiceTypeName = serviceTypeName;
            WorkerAppNamePrefix = workerAppNamePrefix;
        }
        public readonly string AppTypeName;
        public readonly string AppTypeVersion;
        public readonly string ServiceTypeName;
        public readonly string WorkerAppNamePrefix;
    }
    public class CtrlSvc : StatefulService
    {
        public static readonly string s_OperationQueueName = "Opeartions";
        public static readonly string s_WorkerDictionaryName = "Workers";
        public static readonly int s_MaxWorkerOpeartionRetry = 5;

        public static WorkerAppDefinition DefaultWorkerAppDefinition { get; private set; }

        public CtrlSvc()
        {
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            SetAppTypeDefaults();
        }

        /// <summary>
        /// loads default worker app type default name and version
        /// from and configuration and saves them for later use 
        /// by OpeartionHandlers
        /// </summary>
        private void SetAppTypeDefaults()
        {
           

            var settingsFile = ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
            var defaultsSection = settingsFile.Sections["WorkerDefaults"];

            var workerAppDef = new WorkerAppDefinition
                                                (
                                                 defaultsSection.Parameters["AppTypeName"].Value,
                                                 defaultsSection.Parameters["AppTypeVersion"].Value,
                                                 defaultsSection.Parameters["ServiceTypeName"].Value,
                                                 defaultsSection.Parameters["WorkerAppNamePrefix"].Value
                                                );

            CtrlSvc.DefaultWorkerAppDefinition = workerAppDef;
        }

        protected override ICommunicationListener CreateCommunicationListener()
        {
            // create a new Owin listener that uses our spec
            // which needs state manager (to be injected in relevant controllers). 
            var spec = new CtrlSvcOwinListenerSpec();
            spec.StateManager = this.StateManager;
            return new OwinCommunicationListener(spec);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            SetAppTypeDefaults();

            // subscribe to configuration changes
            base.ServiceInitializationParameters.CodePackageActivationContext.ConfigurationPackageModifiedEvent += CodePackageActivationContext_ConfigurationPackageModifiedEvent;


            var operationQ = await StateManager.GetOrAddAsync<IReliableQueue<WorkerOperation>>(CtrlSvc.s_OperationQueueName);
            var workers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Worker>>(CtrlSvc.s_WorkerDictionaryName);

            // todo: should we give API controller breathing
            // room to enque opeartions

            // pump and execute workeroperation from the queue
            while (!cancellationToken.IsCancellationRequested)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {

                    try
                    {
                        var result = await operationQ.TryDequeueAsync(tx,
                                                                 TimeSpan.FromMilliseconds(100),
                                                                 cancellationToken);
                        if (result.HasValue)
                        {

                            var handler = WorkerOpeartionHandlerFactory.Create(this.StateManager, result.Value);
                            await handler.HandleOpeartion(tx);
                            await tx.CommitAsync();
                        }
                    }
                    catch (TimeoutException toe)
                    {
                        // we failed to dequeue in time
                        // todo: log !
                    }
                    catch (Exception E)
                    {
                        Debugger.Break();

                        //todo: log!
                        // this is bad!
                    }
                   
                }
             }
           }
      }
}
