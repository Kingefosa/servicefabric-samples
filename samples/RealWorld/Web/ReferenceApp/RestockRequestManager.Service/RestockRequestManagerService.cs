// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace RestockRequestManager.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;
    using RestockRequestManager.Domain;

    internal class RestockRequestManagerService : StatefulService, IRestockRequestManager, IRestockRequestEvents
    {
        //TODO: Look@ use of these variables.
        private const string ItemIdToActorIdMapName = "actorIdToMapName"; //Name of ItemId-ActorId IReliableDictionary
        private const string CompletedRequestsQueueName = "completedRequests"; //Name of CompletedRequests IReliableQueue
        private const string InventoryServiceName = "InventoryService";
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
            var completedRequests = await this.StateManager.GetOrAddAsync<IReliableQueue<RestockRequest>>(CompletedRequestsQueueName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await completedRequests.EnqueueAsync(tx, request);
                await tx.CommitAsync();
            }

            var restockRequestActor = ActorProxy.Create<IRestockRequestActor>(actorId, this.ApplicationName);
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
            var requestDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, ActorId>>(ItemIdToActorIdMapName);

            ActorId actorId = null; //QUESTION: Why do we always create a new ActorId here?

            using (var tx = this.StateManager.CreateTransaction())
            {
                actorId = await requestDictionary.GetOrAddAsync(tx, request.ItemId, ActorId.NewId());
                await tx.CommitAsync();
            }

            // Create actor proxy and send the request
            var restockRequestActor = ActorProxy.Create<IRestockRequestActor>(actorId, this.ApplicationName);
            ServiceEventSource.Current.Message("Actor proxy created for a new restock request"); //TEST MSG

            try
            {
                await restockRequestActor.AddRestockRequestAsync(request);

                // Successfully added, register for event notifications for completion
                await restockRequestActor.SubscribeAsync<IRestockRequestEvents>(this);
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("RestockRequestManagerService: Actor rejected {0}: {1}", request, ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("RestockRequestManagerService: Exception {0}: {1}", request, ex));
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
            var completedRequests = await this.StateManager.GetOrAddAsync<IReliableQueue<RestockRequest>>(CompletedRequestsQueueName);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Every batching interval, get the completed requests and send them back to inventory service
                using (var tx = this.StateManager.CreateTransaction())
                {
                    IList<RestockRequest> batch = new List<RestockRequest>();

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    while (stopWatch.Elapsed < CompletedRequestsBatchInterval && !cancellationToken.IsCancellationRequested)
                    {
                        // Create a list of batched requests that need to be sent to the Inventory service.
                        var result = await completedRequests.TryDequeueAsync(tx, TxTimeout, cancellationToken);
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

                    if (batch.Count > 0)
                    {
                        ServiceEventSource.Current.Message(string.Format("RestockRequestManagerService: Batch {0} completed requests", batch.Count));

                        // TODO: need to go to correct partition
                        // For now, the inventory is not partitioned, so always go to first partition
                        var builder = new ServiceUriBuilder(InventoryServiceName);

                        var inventoryService = ServiceProxy.Create<IInventoryService>(0, builder.ToUri());
                        await inventoryService.AddStockAsync(batch);
                    }

                    // This commits the dequeue operations.
                    // If the request to add the stock to the inventory service throws, this commit will not execute
                    // and the items will remain on the queue, so we can be sure that we didn't dequeue items
                    // that didn't get saved successfully in the inventory service.
                    // However there is a very small chance that the stock was added to the inventory service successfully,
                    // but service execution stopped before reaching this commit (machine crash, for example).
                    await tx.CommitAsync();
                }

                await Task.Delay(CompletedRequestsBatchInterval, cancellationToken);
            }
        }
    }
}