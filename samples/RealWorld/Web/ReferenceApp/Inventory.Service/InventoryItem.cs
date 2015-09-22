// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Inventory.Service
{
    using System;
    using System.Threading.Tasks;
    using Common;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;
    using RestockRequestManager.Domain;

    [Serializable]
    public sealed class InventoryItem
    {
        private const string RestockRequestManagerServiceName = "RestockRequestManager";

        //private member variables
        private readonly Guid m_Id; //Unique identifier for each item style
        private int m_availableStock; //Quantity in stock
        private decimal m_price; //Price 
        private string m_description; //Brief description of product for display on website
        private int m_restockThreshold; //Available stock at which we should reorder
        private int m_maxStockThreshold; //Maximum number of units that can be in-stock at any time (due to physicial/logistical constraints in warehouses)
        private bool m_onReorder; //True if item is on reorder

        //constructor
        public InventoryItem(Guid id, string Description, decimal Price, int AvailableStock, int RestockThreshold, int MaxStockThreshold, bool onReorder)
        {
            this.m_Id = id;
            this.m_description = Description;
            this.m_price = Price;
            this.m_availableStock = AvailableStock;
            this.m_restockThreshold = RestockThreshold;
            this.m_maxStockThreshold = MaxStockThreshold;
            this.m_onReorder = false;
        }

        /// <summary>
        /// Returns an InventoryItemView object, which contains only external, customer-facing data about an item in inventory.
        /// </summary>
        /// <param name="item"></param>
        public static implicit operator InventoryItemView(InventoryItem item) =>
            new InventoryItemView
            {
                Id = item.m_Id,
                Price = item.m_price,
                Description = item.m_description,
                CustomerAvailableStock = item.m_availableStock - item.m_restockThreshold //Business logic: constraint to reduce overordering.
            };


        //TEST METHOD
        public Task<string> PrintInventoryItem()
        {
            string result =
                string.Format(
                    "For Guid {0}: {1} at a price of {2} with {3} available items at a restock threshold of {4} and with max stocking threshold of {5}.",
                    this.m_Id,
                    this.m_description,
                    this.m_price.ToString(),
                    this.m_availableStock.ToString(),
                    this.m_restockThreshold.ToString(),
                    this.m_maxStockThreshold.ToString());
            return Task.FromResult(result);
        }


        //public accessor methods

        /// <summary>
        /// Increments the quantity of a particular item in inventory.
        /// <param name="quantity"></param>
        /// <returns>int: Returns the quantity that has been added to stock</returns>
        public Task<int> AddStock(int quantity)
        {
            ServiceEventSource.Current.Message(
                "AddStock method for inventory item being executed. m_availableStock before increment is {0}, amt requested to add is {1}.",
                this.m_availableStock,
                quantity); //TEST MSG

            if ((this.m_availableStock + quantity) > this.m_maxStockThreshold)
                //Business Logic: The quantity that the client is trying to add to stock is greater than what can be physically accommodated in a Fabrikam Warehouse
            {
                ServiceEventSource.Current.Message(
                    "YOU HAVE EXCEEDED THE MAXIMUM QUANTITY TO RESTOCK FOR THIS REORDER REQUEST. Please override the max quantity for this item or consider holding in overstock.");
                //TEST MSG

                this.m_availableStock += (this.m_maxStockThreshold - this.m_availableStock);
                //Business logic: For now, this method only adds new units up maximum stock threshold. In an expanded version of this application, we
                //could include tracking for the remaining units and store information about overstock elsewhere. 
            }
            else
            {
                this.m_availableStock += quantity; //Add stock to inventory
            }
            this.m_onReorder = false;
            ServiceEventSource.Current.Message("After increment, m_availableStock is {0}.", this.m_availableStock); //TEST MSG
            return Task.FromResult(quantity);
        }


        /// <summary>
        /// Decrements the quantity of a particular item in inventory and ensures the restockThreshold hasn't
        /// been breached. If so, a RestockRequest is generated in CheckThreshold. 
        /// 
        /// If there is sufficient stock of an item, then the integer returned at the end of this call should be the same as quantityDesired. 
        /// In the event that there is not sufficient stock available, the method will remove whatever stock is available and return that quantity to the client.
        /// In this case, it is the responsibility of the client to determine if the amount that is returned is the same as quantityDesired.
        /// It is invalid to pass in a negative number. 
        /// </summary>
        /// <param name="quantityDesired"></param>
        /// <returns>int: Returns the number actually removed from stock. </returns>
        /// 
        public async Task<int> RemoveStock(int quantityDesired)
        {
            ServiceEventSource.Current.Message(
                "RemoveStock method for inventory item being executed. m_availableStock before decrement is {0}, with {1} items requested to remove.",
                this.m_availableStock,
                quantityDesired); //TEST MESSAGE

            int removed = Math.Min(quantityDesired, this.m_availableStock); //Assumes quantityDesired is a positive integer

            ServiceEventSource.Current.Message("The number of items that will be removed is {0}.", removed); //TEST MSG

            this.m_availableStock -= removed;
            ServiceEventSource.Current.Message("Checking restock threshold now..."); //TEST MSG

            await this.CheckThreshold(); //check for reorder

            ServiceEventSource.Current.Message("Exited check threshold method, returning now"); //TEST MSG
            return removed;
        }

        /// <summary>
        /// Checks to ensure the quantity available has not dropped below the restockThreshold and that the item is not already on reorder. 
        /// If a restock is necessary, calls the RestockRequestManager service using a ServiceProxy to restock the item in the particular amount requested. 
        /// </summary>
        /// <returns>void</returns>
        private async Task CheckThreshold()
        {
            ServiceEventSource.Current.Message("Checking threshold for reorder now..."); //TEST MSG
            ServiceEventSource.Current.Message(
                "Available Stock: {0}. Reorder Threshold: {1}. Reorder Status? {2}.",
                this.m_availableStock,
                this.m_restockThreshold,
                this.m_onReorder.ToString()); //TEST MSG

            if ((this.m_availableStock <= this.m_restockThreshold) && !this.m_onReorder)
                //Check if stock is below restockThreshold and if the item is not already on reorder
            {
                ServiceEventSource.Current.Message("Placing order through restock request manager..."); //TEST MSG

                ServiceUriBuilder builder = new ServiceUriBuilder(RestockRequestManagerServiceName);

                IRestockRequestManager restockRequestManagerClient = ServiceProxy.Create<IRestockRequestManager>(
                    0,
                    builder.ToUri());

                ServiceEventSource.Current.Message("executed ServiceProxy Create method"); //TEST MSG

                RestockRequest newRequest = new RestockRequest(this.m_Id, (this.m_maxStockThreshold - this.m_availableStock));
                //Business Logic: we reduce the quantity passed in to RestockRequest
                //to ensure we don't overorder

                ServiceEventSource.Current.Message(newRequest.ToString()); //TEST MSG

                try
                {
                    await restockRequestManagerClient.AddRestockRequestAsync(newRequest); //Place RestockRequest
                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.Message(e.ToString());
                }

                this.m_onReorder = true; //InventoryItem marked as on-reorder.
                ServiceEventSource.Current.Message("Order placed. m_onReorder is {0}.", this.m_onReorder.ToString()); //TEST MSG
            }
            else
            {
                ServiceEventSource.Current.Message("No restock order placed. One or all of conditions not met. Exiting CHECKTHRESHOLD function now.");
                //TEST MSG
            }
        }
    }
}