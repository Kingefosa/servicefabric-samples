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
        private IReliableStateManager stateManager;

        /// <summary>
        /// Poor-man's dependency injection for now until the API supports proper injection of IReliableStateManager.
        /// This is the constructor called by the FabricRuntime.
        /// </summary>
        public InventoryService()
        {
        }

        /// <summary>
        /// Poor-man's dependency injection for now until the API supports proper injection of IReliableStateManager.
        /// This constructor is used in unit tests to inject a different state manager.
        /// </summary>
        /// <param name="stateManager"></param>
        public InventoryService(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Used internally to generate inventory items and adds them to the ReliableDict we have.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task CreateInventoryItemAsync(InventoryItem item)
        {
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await inventoryItems.AddAsync(tx, item.Id, item);
                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(this, "Created inventory item: {0}", item);
        }

        /// <summary>
        /// Tries to add the given quantity to the inventory item with the given ID without going over the maximum quantity allowed for an item.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns>The quantity actually added to the item.</returns>
        public async Task<int> AddStockAsync(InventoryItemId itemId, int quantity)
        {
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            int quantityAdded = 0;

            ServiceEventSource.Current.ServiceMessage(this, "Received add stock request. Item: {0}. Quantity: {1}.", itemId, quantity);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                // Try to get the InventoryItem for the ID in the request.
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);

                // We can only update the stock for InventoryItems in the system - we are not adding new items here.
                if (item.HasValue)
                {
                    // Update the stock quantity of the item.
                    // This only updates the copy of the Inventory Item that's in local memory here;
                    // It's not yet saved in the dictionary.
                    quantityAdded = item.Value.AddStock(quantity);

                    // We have to store the item back in the dictionary in order to actually save it.
                    // This will then replicate the updated item for
                    await inventoryItems.SetAsync(tx, item.Value.Id, item.Value);
                }

                // nothing will happen unless we commit the transaction!
                await tx.CommitAsync();

                ServiceEventSource.Current.ServiceMessage(
                    this,
                    "Add stock complete. Item: {0}. Added: {1}. Total: {2}",
                    item.Value.Id,
                    quantityAdded,
                    item.Value.AvailableStock);
            }


            return quantityAdded;
        }

        /// <summary>
        /// Removes the given quantity of stock from an in item in the inventory.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>int: Returns the quantity removed from stock.</returns>
        public async Task<int> RemoveStockAsync(InventoryItemId itemId, int quantity)
        {
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            int removed = 0;

            ServiceEventSource.Current.ServiceMessage(this, "Received remove stock request. Item: {0}. Quantity: {1}.", itemId, quantity);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                // Try to get the InventoryItem for the ID in the request.
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);

                // We can only remove stock for InventoryItems in the system.
                if (item.HasValue)
                {
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

                ServiceEventSource.Current.ServiceMessage(
                    this,
                    "Removed stock complete. Item: {0}. Removed: {1}. Remaining: {2}",
                    item.Value.Id,
                    removed,
                    item.Value.AvailableStock);
            }

            return removed;
        }

        public async Task<bool> IsItemInInventoryAsync(InventoryItemId itemId)
        {
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.stateManager.CreateTransaction())
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
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            ServiceEventSource.Current.Message("Called GetCustomerInventory to return InventoryItemView");

            return inventoryItems.Select(x => (InventoryItemView) x.Value).Where(x => x.CustomerAvailableStock > 0);
        }

        /// <summary>
        /// NOTE: This should not be used in published MVP code. 
        /// This function allows us to remove inventory items from inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task DeleteInventoryItemAsync(InventoryItemId itemId)
        {
            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await inventoryItems.TryRemoveAsync(tx, itemId);
                await tx.CommitAsync();
            }
        }

        protected override IReliableStateManager CreateReliableStateManager()
        {
            if (this.stateManager == null)
            {
                this.stateManager = base.CreateReliableStateManager();
            }
            return this.stateManager;
        }

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
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("InventoryService ReliableDictionary successfully created");
            ServiceEventSource.Current.Message("Adding dummy items.");

            await Task.WhenAll(
                this.CreateInventoryItemAsync(new InventoryItem("Bioluminescent Dress", 14.99M, 2000, 200, 2000)),
                this.CreateInventoryItemAsync(new InventoryItem("Electrifying Lightning Skirt", 29.99M, 1500, 150, 1500)),
                this.CreateInventoryItemAsync(new InventoryItem("Blacklight Striped Trousers", 34.99M, 3000, 300, 3000)));

            IReliableDictionary<InventoryItemId, InventoryItem> inventoryItems =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<InventoryItemId, InventoryItem>>(InventoryItemDictionaryName);

            while (!cancellationToken.IsCancellationRequested)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Checking inventory stock for {0} items.", await inventoryItems.GetCountAsync());

                foreach (InventoryItem item in inventoryItems.Select(x => x.Value))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        //Check if stock is below restockThreshold and if the item is not already on reorder
                        if ((item.AvailableStock <= item.RestockThreshold) && !item.OnReorder)
                        {
                            ServiceUriBuilder builder = new ServiceUriBuilder(RestockRequestManagerServiceName);

                            IRestockRequestManager restockRequestManagerClient = ServiceProxy.Create<IRestockRequestManager>(0, builder.ToUri());

                            // we reduce the quantity passed in to RestockRequest to ensure we don't overorder   
                            RestockRequest newRequest = new RestockRequest(item.Id, (item.MaxStockThreshold - item.AvailableStock));

                            InventoryItem updatedItem = new InventoryItem(
                                item.Description,
                                item.Price,
                                item.AvailableStock,
                                item.RestockThreshold,
                                item.MaxStockThreshold,
                                true);

                            using (ITransaction tx = this.stateManager.CreateTransaction())
                            {
                                await inventoryItems.TryUpdateAsync(tx, item.Id, updatedItem, item);

                                await restockRequestManagerClient.AddRestockRequestAsync(newRequest);

                                await tx.CommitAsync();

                                ServiceEventSource.Current.ServiceMessage(
                                    this,
                                    "Restock order placed. Item ID: {0}. Quantity: {1}",
                                    newRequest.ItemId,
                                    newRequest.Quantity);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, "Failed to place restock order for item {0}. {1}", item.Id, e.ToString());
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}