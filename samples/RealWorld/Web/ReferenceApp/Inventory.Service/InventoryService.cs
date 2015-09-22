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
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;

    public class InventoryService : StatefulService, IInventoryService
    {
        //TEST FUNCTION: Prints the Customer View of the Store to the Service Event Source. 
        public async Task CheckCustomerView(IEnumerable<InventoryItemView> custView)
        {
            ServiceEventSource.Current.Message("checking the customer view of objects now");
            foreach (InventoryItemView item in custView)
            {
                ServiceEventSource.Current.Message(
                    string.Format(
                        "For Guid {0} we sell item {1} at price {2} with customer available stock of {3}",
                        item.Id.ToString(),
                        item.Description,
                        item.Price.ToString(),
                        item.CustomerAvailableStock.ToString()));
            }
        }

        /// <summary>
        /// Used internally to generate inventory items and adds them to the ReliableDict we have.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="description"></param>
        /// <param name="price"></param>
        /// <param name="availableStock"></param>
        /// <param name="restockThreshold"></param>
        /// <param name="maxProductionThreshold"></param>
        /// <returns></returns>
        public async Task CreateInventoryItemAsync(
            Guid itemId, string description, decimal price, int availableStock, int restockThreshold, int maxProductionThreshold)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await inventoryItems.AddAsync(
                    tx,
                    itemId,
                    new InventoryItem(itemId, description, price, availableStock, restockThreshold, maxProductionThreshold, false));
                await tx.CommitAsync();

                //TEST CODE
                ServiceEventSource.Current.Message(
                    string.Format(
                        "Created inventory item {0}: {1} at a price of {2} with {3} available items at a restock threshold of {4} with max production of {5}.",
                        itemId.ToString(),
                        description,
                        price.ToString(),
                        availableStock.ToString(),
                        restockThreshold.ToString(),
                        maxProductionThreshold.ToString()));
            }
        }

        /// <summary>
        /// This function takes a list of RestockRequest objects and adds them to inventory asynchronously.
        /// </summary>
        /// <param name="requests"></param>
        /// <returns></returns>
        public async Task RestockRequestsCompleted(IList<RestockRequest> requests)
        {
            ServiceEventSource.Current.Message(string.Format("Received update for {0} completed requests", requests.Count));

            //[OANA] TODO: optimize to update requests in a single tx or start in parallel       
            foreach (RestockRequest request in requests)
            {
                await this.AddStock(request);
            }
        }


        /// <summary>
        /// TODO: This is still confusing, we may need to change. 
        /// Callback function used in UpdateQuantityOpAsync to add items to inventory. 
        /// Call made using C# 6.0 lambdas. 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <returns>int: Returns the quantity removed from stock.</returns>
        public Task<int> RemoveStock(Guid itemId, int quantity)
            => this.UpdateQuantityOpAsync(itemId, ii => ii.RemoveStock(quantity));

        public async Task<bool> IsItemInInventoryAsync(Guid itemId)
        {
            bool returnValue = false;

            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);
                if (item.HasValue)
                {
                    returnValue = true;
                }
            }

            return returnValue;
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
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");
            ServiceEventSource.Current.Message("Called GetCustomerInventory to return InventoryItemView");
            return inventoryItems.Select(kvp => (InventoryItemView) kvp.Value).Where(x => x.CustomerAvailableStock > 0);
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
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await inventoryItems.TryRemoveAsync(tx, itemId);
                await tx.CommitAsync();
            }
        }

        //IMPORTANT: Whenever we want to access a Reliable Collection, we access its state using the State Manager, referencing the state by name.
        //We should not declare a class-level variable for our Reliable Collection and reference the collection in this manner. This may produce
        //unexpected behavior. 


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");
            ServiceEventSource.Current.Message("InventoryService ReliableDictionary successfully created");

            await this.testAll(); //TEST CODE: not necessary to include in final version. 
        }


        /// <summary>
        /// Creates a new communication listener for protocol of our choice.
        /// </summary>
        /// <returns></returns>
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ServiceCommunicationListener<IInventoryService>(this);
        }

        private async Task testAll()
        {
            ServiceEventSource.Current.Message("Now beginning test of inventoryService.");
            Guid[] itemIds = {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()};
            ServiceEventSource.Current.Message(
                "Guids of Items to be created are \n {0} \n {1} \n {2}",
                itemIds[0].ToString(),
                itemIds[1].ToString(),
                itemIds[2].ToString());
            await this.CreateInventoryItemAsync(itemIds[0], "Bioluminescent Dress", 14.99M, 2000, 200, 2000);
            await this.CreateInventoryItemAsync(itemIds[1], "Electrifying Lightning Skirt", 29.99M, 1500, 150, 1500);
            await this.CreateInventoryItemAsync(itemIds[2], "Blacklight Striped Trousers", 34.99M, 3000, 300, 3000);

            //Test to see that these are all stored correctly in IReliableDictionary: 
            await this.checkDictionary(itemIds);


            /*int removed = await RemoveStock(itemIds[0], 1000);
            if (removed != 1000)
            {
                ServiceEventSource.Current.Message("ERROR: There is a bug in the removeStock logic. Instead of 0, your remaining stock to remove is {0}", removed);
            } else
            {
                ServiceEventSource.Current.Message("Congrats! Your first removeStock call performed correctly. Now, to test our restock threshold. Right now, the available stock for itemId {0} should be 1000.", itemIds[0]);
            }
            await checkDictionary(new Guid[] { itemIds[0] });
            int removedToThreshold = await RemoveStock(itemIds[0], 800);
            if (removedToThreshold != 800)
            {
                ServiceEventSource.Current.Message("ERROR: There is probably a bug in your checkThreshold method. Instead of 200, your remaining stock to remove is {0}. Further investigation is necessary.", removedToThreshold);
            }
            await checkDictionary(new Guid[] { itemIds[0], itemIds[1], itemIds[2] });
            int removedPastThreshold = await RemoveStock(itemIds[0], 1);
            if (removedPastThreshold != 1)
            {
                ServiceEventSource.Current.Message("ERROR: There is probably a bug in your checkThreshold method. Instead of 200, your remaining stock to remove is {0}. Further investigation is necessary.", removedPastThreshold);
            }
            await checkDictionary(new Guid[] { itemIds[0] });

            //Now to simulate adding stock back in
            ServiceEventSource.Current.Message("Beginning check of ADD STOCK methods with locally created restockrequests.");
            RestockRequest request1 = new RestockRequest(itemIds[0], 1000);
            RestockRequest request2 = new RestockRequest(itemIds[1], 10);
            ServiceEventSource.Current.Message("Requesting 1000 items for GUID {0}, 10 items for GUID {1}", itemIds[0].ToString(), itemIds[1].ToString());
            List<RestockRequest> requestList = new List<RestockRequest>();
            requestList.Add(request1);
            requestList.Add(request2);
            await RestockRequestsCompleted(requestList);
            await checkDictionary(new Guid[] { itemIds[0], itemIds[1] });

            //Check conversion from Inventory Service Items to Customer Storeview Items: 
            ServiceEventSource.Current.Message("Now checking conversion to InventoryItemView from InventoryItem...");
            IEnumerable<InventoryItemView> customerView = await GetCustomerInventory();
            checkCustomerView(customerView); */


            //look up an item and print information about it: this test confirms that what we have added is what is represented in state
            //remove stock from an item above the CheckOrder threshold and check return values
            //Remove stock from an item below the checkorder threshold and check return values --> put in a stub so it doesn't call restock request, or comment it out or whatever. 
            //create a restock request locally, and then try to add stock back to the inventory service. 
            //Less important: test adding inventory. 
        }

        //TEST FUNCTION: Checks that the items in the dictionary match what has been placed in them through the
        //CreateItems call 
        private async Task checkDictionary(Guid[] itemIds)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");

            ServiceEventSource.Current.Message("Trying to print out values for each item in dictionary...");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                foreach (Guid id in itemIds)
                {
                    ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, id);
                    if (item.HasValue)
                    {
                        string itemToPrint = await item.Value.PrintInventoryItem();
                        ServiceEventSource.Current.Message(itemToPrint);
                    }
                }
            }
        }

        /// <summary>
        /// TODO: This is still confusing, we may need to change. 
        /// Callback function used in UpdateQuantityOpAsync to add items to inventory. 
        /// Call made using C# 6.0 lambdas. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns the quantity to be added per the restock request. </returns>
        private Task<int> AddStock(RestockRequest request)
            => this.UpdateQuantityOpAsync(request.ItemId, ii => ii.AddStock(request.Quantity));

        /// <summary>
        /// This is a generic function accepting a Guid to access an inventory item and and a callback function
        /// that performs an update operation (add or remove) on the inventory service. The integer quantity 
        /// that is returned at 
        /// the end of this method represents the number of items that have been added or removed from stock.
        /// This function returns return an integer quantity that is a result of an operation, update, performed
        /// on an InventoryItem in the Service. 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="update"></param>
        /// <returns>Integer quantity that is either added to inventory or removed from it.</returns>
        private async Task<int> UpdateQuantityOpAsync(Guid itemId, Func<InventoryItem, Task<int>> update)
        {
            IReliableDictionary<Guid, InventoryItem> inventoryItems =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, InventoryItem>>("inventoryItems");

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                int quantity = 0;
                ConditionalResult<InventoryItem> item = await inventoryItems.TryGetValueAsync(tx, itemId);
                if (item.HasValue)
                {
                    ServiceEventSource.Current.Message("Update Quantity in process  " + item.Value.PrintInventoryItem());
                    quantity = await update(item.Value); //Callback function
                    await inventoryItems.SetAsync(tx, itemId, item.Value);
                }
                await tx.CommitAsync();
                return quantity;
            }
        }
    }
}