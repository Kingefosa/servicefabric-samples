// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace CustomerOrder.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using CustomerOrder.Domain;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Services;

    internal class CustomerOrderActor : Actor<CustomerOrderActorState>, ICustomerOrderActor, IRemindable
    {
        private const string InventoryServiceName = "InventoryService";


        /// <summary>
        /// This method accepts a list of CustomerOrderItems, representing a customer order, and sets the actor's state
        /// to reflect the status and contents of the order. Then, the order is fulfilled with a private FulfillOrder call
        /// that abstracts away the entire backorder process from the user. 
        /// </summary>
        /// <param name="orderList"></param>
        /// <returns></returns>
        async Task<bool> ICustomerOrderActor.SubmitOrderAsync(List<CustomerOrderItem> orderList)
        {
            this.State.OrderedItems = orderList;
            this.State.FulfilledItems = new Dictionary<Guid, int>();
            this.State.BackorderedItems = new List<Guid>();
            this.State.Status = CustomerOrderStatus.Submitted;

            ActorEventSource.Current.ActorMessage(this, this.State.ToString());

            await
                this.RegisterReminder(
                    CustomerOrderReminderNames.FulfillOrderReminder,
                    null,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10),
                    ActorReminderAttributes.None);

            return true;
        }

        /// <summary>
        /// Returns the status of the Customer Order. 
        /// </summary>
        /// <returns></returns>
        Task<string> ICustomerOrderActor.GetStatusAsync()
        {
            return Task.FromResult(this.State.Status.ToString());
        }

        Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case CustomerOrderReminderNames.FulfillOrderReminder:
                    return this.FulfillOrder();
                case CustomerOrderReminderNames.BackorderReminder:
                    return this.FulfillBackorder();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Initializes CustomerOrderActor state. Because an order actor will only be activated
        /// once in this scenario and never used again, when we initiate the actor's state we
        /// change the order's status to "Confirmed," and do not need to check if the actor's 
        /// state was already set to this. 
        /// </summary>
        public override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new CustomerOrderActorState();
                this.State.Status = CustomerOrderStatus.Confirmed;
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// This method takes in a list of CustomerOrderItem objects. Using a Service Proxy to access the Inventory Service,
        /// the method iterates onces through the order and tries to remove the quantity specified in the order from inventory. 
        /// If the inventory has insufficient stock to remove the requested amount for a particular item, the entire order is 
        /// marked as backordered and the item in question is added to a "backordered" item list, which is fulfilled in a separate 
        /// method. 
        /// 
        /// In its current form, this application addresses the question of race conditions to remove the same item by making a rule
        /// that no order ever fails. While an item that is displayed in the store may not be available any longer by the time an order is placed,
        /// the automatic restock policy instituted in the Inventory Service means that our FulfillOrder method and its sub-methods can continue to 
        /// query the Inventory Service on repeat (with a timer in between each cycle) until the order is fulfilled. 
        /// 
        /// TODO: Figure out behavior for a crash in the middle of this function. 
        /// </summary>
        /// <returns></returns>
        private async Task FulfillOrder()
        {
            this.State.Status = CustomerOrderStatus.InProcess;

            ServiceUriBuilder builder = new ServiceUriBuilder(InventoryServiceName);

            List<CustomerOrderItem> orderList = this.State.OrderedItems;
            IInventoryService inventoryServiceClient = ServiceProxy.Create<IInventoryService>(0, builder.ToUri());

            //First, check all items are listed in inventory.  
            //This will avoid infinite backorder status.
            foreach (CustomerOrderItem item in orderList)
            {
                if ((await inventoryServiceClient.IsItemInInventoryAsync(item.ItemId)) == false)
                {
                    this.State.Status = CustomerOrderStatus.Canceled;
                    return;
                }
            }

            //We loop through the customer order list. 
            //For every item that cannot be fulfilled, we add to backordered. 
            foreach (CustomerOrderItem item in orderList)
            {
                int numberItemsRemoved = await inventoryServiceClient.RemoveStockAsync(item.ItemId, item.Quantity);
                this.State.FulfilledItems.Add(item.ItemId, numberItemsRemoved);
                if (numberItemsRemoved < item.Quantity)
                {
                    this.State.BackorderedItems.Add(item.ItemId);
                }
            }

            //Process any backordered item
            if (this.State.BackorderedItems.Count > 0)
            {
                this.State.Status = CustomerOrderStatus.Backordered;
                await
                    this.RegisterReminder(
                        CustomerOrderReminderNames.BackorderReminder,
                        null,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMinutes(1),
                        ActorReminderAttributes.None);
            }
            else
            {
                this.State.Status = CustomerOrderStatus.Shipped;
            }

            //Remove fulfill order reminder so Actor can be gargabe collected.
            IActorReminder orderReminder = this.GetReminder(CustomerOrderReminderNames.FulfillOrderReminder);
            await this.UnregisterReminder(orderReminder);
        }

        /// <summary>
        /// This method fulfills backordered items. The method will continue to cycle through the items not fulfilled until the correct amount 
        /// of inventory can be removed for each item on the list. There is a time to separate each cycle to anticipate inventory restock. 
        /// </summary>
        /// <param name="backorderList"></param>
        /// <returns></returns>
        private async Task FulfillBackorder()
        {
            ServiceUriBuilder builder = new ServiceUriBuilder(InventoryServiceName);

            IInventoryService inventoryServiceClient = ServiceProxy.Create<IInventoryService>(0, builder.ToUri());
            List<Guid> backorderItemsFulfilled = new List<Guid>();

            foreach (Guid itemId in this.State.BackorderedItems)
            {
                //Try to fulfill backorder
                CustomerOrderItem itemToFulfill = this.State.OrderedItems.Single(item => item.ItemId == itemId);
                int numberItemsRemoved = await inventoryServiceClient.RemoveStockAsync(itemId, itemToFulfill.Quantity - this.State.FulfilledItems[itemId]);

                //Update fulfilled status and remove backorderitem if needed.
                this.State.FulfilledItems[itemId] += numberItemsRemoved;
                if (this.State.FulfilledItems[itemId] >= itemToFulfill.Quantity)
                {
                    backorderItemsFulfilled.Add(itemId);
                }
            }

            //Remove any items that are completely fulfilled from the backorder list.
            backorderItemsFulfilled.ForEach(item => this.State.BackorderedItems.Remove(item));

            if (this.State.BackorderedItems.Count == 0)
            {
                this.State.Status = CustomerOrderStatus.Shipped;

                //now that we're done processing the backorder, remove the reminder
                IActorReminder backorderReminder = this.GetReminder(CustomerOrderReminderNames.BackorderReminder);
                await this.UnregisterReminder(backorderReminder);
            }
        }
    }
}