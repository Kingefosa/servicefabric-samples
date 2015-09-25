// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Inventory.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;
    using RestockRequestManager.Domain;

    internal class InventoryService : StatefulService, IInventoryService
    {
        private const string InventoryItemDictionaryName = "inventoryItems";

        private const string RestockRequestManagerServiceName = "RestockRequestManager";

        /// <summary>
        /// Creates a new communication listener for protocol of our choice.
        /// </summary>
        /// <returns></returns>
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ServiceCommunicationListener<IInventoryService>(this);
        }

        /// <summary>
        /// Populates the inventory with some dummy items.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("InventoryService ReliableDictionary successfully created");
            ServiceEventSource.Current.Message("Adding dummy items.");

            return Task.WhenAll(
                this.CreateInventoryItemAsync(new InventoryItem("Bioluminescent Dress", 14.99M, 2000, 200, 2000)),
                this.CreateInventoryItemAsync(new InventoryItem("Electrifying Lightning Skirt", 29.99M, 1500, 150, 1500)),
                this.CreateInventoryItemAsync(new InventoryItem("Blacklight Striped Trousers", 34.99M, 3000, 300, 3000)));
        }

        /// <summary>
        /// Used internally to generate inventory items and adds them to the ReliableDict we have.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task CreateInventoryItemAsync(InventoryItem item)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await inventoryItems.AddAsync(tx, item.Id, item);
                await tx.CommitAsync();
            }

            ServiceEventSource.Current.Message(String.Format("Created inventory item: {0}", item));
        }

        /// <summary>
        /// This function takes a list of RestockRequest objects and adds them to inventory asynchronously.
        /// </summary>
        /// <param name="requests"></param>
        /// <returns></returns>
        public async Task<int> AddStockAsync(IEnumerable<RestockRequest> requests)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            int quantity = 0;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                foreach (RestockRequest request in requests)
                {
                    // Try to get the InventoryItem for the ID in the request.
                    ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, request.ItemId);

                    // We can only update the stock for InventoryItems in the system - we are not adding new items here.
                    if (item.HasValue)
                    {
                        ServiceEventSource.Current.Message("Adding quantity for item {0} by {1}.", item.Value.Id, request.Quantity);
                        
                        // Update the stock quantity of the item.
                        // This only updates the copy of the Inventory Item that's in local memory here;
                        // It's not yet saved in the dictionary.
                        quantity = item.Value.AddStock(request.Quantity);

                        // We have to store the item back in the dictionary in order to actually save it.
                        // This will then replicate the updated item for
                        await inventoryItems.SetAsync(tx, item.Value.Id, item.Value);
                    }
                }

                // nothing will happen unless we commit the transaction!
                await tx.CommitAsync();
            }

            ServiceEventSource.Current.Message(string.Format("Received update for {0} completed requests", quantity));
            return quantity;
        }

        /// <summary>
        /// Removes the given quantity of stock from an in item in the inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns>int: Returns the quantity removed from stock.</returns>
        public async Task<int> RemoveStockAsync(Guid itemId, int quantity)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            int removed = 0;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                // Try to get the InventoryItem for the ID in the request.
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);

                // We can only remove stock for InventoryItems in the system.
                if (item.HasValue)
                {
                    ServiceEventSource.Current.Message("Removing quantity for item {0} by {1}.", item.Value.Id, quantity);

                    // Update the stock quantity of the item.
                    // This only updates the copy of the Inventory Item that's in local memory here;
                    // It's not yet saved in the dictionary.
                    removed = item.Value.RemoveStock(quantity);

                    // We have to store the item back in the dictionary in order to actually save it.
                    // This will then replicate the updated item for
                    await inventoryItems.SetAsync(tx, itemId, item.Value);
                }

                // nothing will happen unless we commit the transaction!
                await tx.CommitAsync();

                await CheckInventoryThreshold(item.Value);
            }


            ServiceEventSource.Current.Message(string.Format("Received update for {0} completed requests", quantity));
            return removed;
        }

        public async Task<bool> IsItemInInventoryAsync(Guid itemId)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);
                return item.HasValue;
            }
        }

        /// <summary>
        /// Retrieves a customer-specific view (defined in the InventoryItemView class in the Fabrikam Common namespace)
        /// af all items in the IReliableDictionary in InventoryService. Only items with a CustomerAvailableStock greater than
        /// zero are returned as a business logic constraint to reduce overordering. 
        /// </summary>
        /// <returns>IEnumerable of InventoryItemView</returns>
        public async Task<IEnumerable<InventoryItemView>> GetCustomerInventoryAsync()
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            ServiceEventSource.Current.Message("Called GetCustomerInventory to return InventoryItemView");

            return inventoryItems.Select(x => (InventoryItemView)x.Value).Where(x => x.CustomerAvailableStock > 0);
        }


        /// <summary>
        /// NOTE: This should not be used in published MVP code. 
        /// This function allows us to remove inventory items from inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task DeleteInventoryItemAsync(Guid itemId)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await inventoryItems.TryRemoveAsync(tx, itemId);
                await tx.CommitAsync();
            }
        }


        /// <summary>
        /// Checks to ensure the quantity available has not dropped below the restockThreshold and that the item is not already on reorder. 
        /// If a restock is necessary, calls the RestockRequestManager service using a ServiceProxy to restock the item in the particular amount requested. 
        /// </summary>
        /// <returns>void</returns>
        private async Task CheckInventoryThreshold(InventoryItem item)
        {
            ServiceEventSource.Current.Message(
                "Available Stock: {0}. Reorder Threshold: {1}. Reorder Status? {2}.",
                item.AvailableStock,
                item.RestockThreshold,
                item.OnReorder.ToString()); //TEST MSG

            //Check if stock is below restockThreshold and if the item is not already on reorder
            if ((item.AvailableStock <= item.RestockThreshold) && !item.OnReorder)
            {
                ServiceUriBuilder builder = new ServiceUriBuilder(RestockRequestManagerServiceName);

                IRestockRequestManager restockRequestManagerClient = ServiceProxy.Create<IRestockRequestManager>(0, builder.ToUri());
               
                // we reduce the quantity passed in to RestockRequest
                // to ensure we don't overorder                
                RestockRequest newRequest = new RestockRequest(item.Id, (item.MaxStockThreshold - item.AvailableStock));
                
                ServiceEventSource.Current.Message(newRequest.ToString()); //TEST MSG

                try
                {
                    await restockRequestManagerClient.AddRestockRequestAsync(newRequest); //Place RestockRequest
                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.Message(e.ToString());
                }

                item.OnReorder = true; //InventoryItem marked as on-reorder.
                ServiceEventSource.Current.Message("Order placed. onReorder is {0}.", item.OnReorder.ToString());
            }
            else
            {
                ServiceEventSource.Current.Message("No restock order placed. One or all of conditions not met. Exiting CHECKTHRESHOLD function now.");
            }
        }
    }
}