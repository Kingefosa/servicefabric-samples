// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace RestockRequestManager.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using global::RestockRequestManager.Domain;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;

    public class RestockRequestManager : StatefulService, IRestockRequestManager, IRestockRequestEvents
    {
        //TODO: Look@ use of these variables.
        private const string ItemIdToActorIdMapName = "actorIdToMapName"; //Name of ItemId-ActorId IReliableDictionary
        private const string CompletedRequestsQueueName = "completedRequests"; //Name of CompletedRequests IReliableQueue
        private const string InventoryServiceName = "/InventoryService";

        private static TimeSpan CompletedRequestsBatchInterval = TimeSpan.FromSeconds(5);
        private static TimeSpan TxTimeout = TimeSpan.FromSeconds(4);

        public string ApplicationName
        {
            get { return this.ServiceInitializationParameters.CodePackageActivationContext.ApplicationName; }
        }

        /// <summary>
        /// This method uses an IReliableQueue to store completed RestockRequests which are later sent to the client using batch processing.
        /// We could send the request immediately but we prefer to minimize traffic back to the Inventory Service by batching multiple requests
        /// in one trip. 
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="request"></param>
        public async void RestockRequestCompleted(ActorId actorId, RestockRequest request)
        {
            IReliableQueue<RestockRequest> completedRequests = await this.StateManager.GetOrAddAsync<IReliableQueue<RestockRequest>>(CompletedRequestsQueueName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await completedRequests.EnqueueAsync(tx, request);
                await tx.CommitAsync();
            }

            IRestockRequestActor restockRequestActor = ActorProxy.Create<IRestockRequestActor>(actorId, this.ApplicationName);
            await restockRequestActor.UnsubscribeAsync<IRestockRequestEvents>(this); //QUESTION:What does this method do?
        }


        /// <summary>
        /// This method activates an actor to fulfill the RestockRequest.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task AddRestockRequestAsync(RestockRequest request)
        {
            ServiceEventSource.Current.Message("Entered AddRestockRequestAsync call in RRM"); //TEST MESG

            //Get dictionary of Restock Requests
            IReliableDictionary<Guid, ActorId> requestDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, ActorId>>(ItemIdToActorIdMapName);

            ActorId actorId = null; //QUESTION: Why do we always create a new ActorId here?

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                actorId = await requestDictionary.GetOrAddAsync(tx, request.ItemId, ActorId.NewId());
                await tx.CommitAsync();
            }

            // Create actor proxy and send the request
            IRestockRequestActor restockRequestActor = ActorProxy.Create<IRestockRequestActor>(actorId, this.ApplicationName);
            ServiceEventSource.Current.Message("Actor proxy created for a new restock request"); //TEST MSG

            try
            {
                await restockRequestActor.AddRestockRequestAsync(request);

                // Successfully added, register for event notifications for completion
                await restockRequestActor.SubscribeAsync<IRestockRequestEvents>(this);
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("RestockRequestManager: Actor rejected {0}: {1}", request, ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("RestockRequestManager: Exception {0}: {1}", request, ex));
                throw;
            }
        }

        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ServiceCommunicationListener<IRestockRequestManager>(this);
        }

        /// <summary>
        /// This method returns completed RestockRequests to the InventoryService by implementing batch processing. 
        /// Based on a pre-defined batching interval, completed requests are dequeued and passed to InventoryService through
        /// the creation of a Service Proxy. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IReliableQueue<RestockRequest> completedRequests = await this.StateManager.GetOrAddAsync<IReliableQueue<RestockRequest>>(CompletedRequestsQueueName);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Every batching interval, get the completed requests and send them back to inventory service
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    IList<RestockRequest> batch = new List<RestockRequest>();
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    while (stopWatch.Elapsed < CompletedRequestsBatchInterval && !cancellationToken.IsCancellationRequested)
                    {
                        // Create a list of batched requests that need to be sent to the Inventory service.
                        // NOTE: since this list is in memory, if the primary crashes before send or the send fails, 
                        // the information is lost and restock request manager can't retry.
                        // To solve this problem, we can either persist the data until after ACK from the Inventory service is received
                        // Or the inventory service can poll for results for the requests that took a long time.
                        ConditionalResult<RestockRequest> result = await completedRequests.TryDequeueAsync(tx, TxTimeout, cancellationToken);
                        if (!result.HasValue)
                        {
                            // All accumulated requests are read
                            break;
                        }
                        else
                        {
                            batch.Add(result.Value);
                        }
                    }

                    await tx.CommitAsync();

                    if (batch.Count > 0)
                    {
                        ServiceEventSource.Current.Message(string.Format("RestockRequestManager: Batch {0} completed requests", batch.Count));

                        // TODO: need to go to correct partition
                        // For now, the inventory is not partitioned, so always go to first partition


                        Uri serviceName = new Uri(this.GetAppServiceName(InventoryServiceName));
                        IInventoryService inventoryService = ServiceProxy.Create<IInventoryService>(0, serviceName);
                        await inventoryService.RestockRequestsCompleted(batch);
                    }
                }

                await Task.Delay(CompletedRequestsBatchInterval, cancellationToken);
            }
        }

        //TODO: Should we call in this manner?
        private string GetAppServiceName(string serviceName)
        {
            return this.ServiceInitializationParameters.CodePackageActivationContext.ApplicationName.TrimEnd('/') + serviceName;
        }
    }
}