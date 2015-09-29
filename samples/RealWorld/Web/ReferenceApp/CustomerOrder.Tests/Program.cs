// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace CustomerOrder.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CustomerOrder.Domain;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Services;

    internal class Program
    {
        private static Uri InventoryServiceName = new Uri("fabric:/FabrikamApplication/InventoryService");
        private static string applicationName = "fabric:/FabrikamApplication";

        public static async Task<Guid> CheckoutTest(List<CustomerOrderItem> cart)
        {
            // you may want to create a guid and an actor from that guid
            var orderId = new Guid();
            var customerOrder = ActorProxy.Create<ICustomerOrderActor>(new ActorId(orderId), applicationName);

            Console.WriteLine("Trying to get initial status of actor...");
            await GetOrderStatus(orderId);

            Console.WriteLine("Theoretically we have created a functioning customerOrder actor");

            // Create actor proxy and send the request
            try
            {
                await customerOrder.SubmitOrderAsync(cart);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(string.Format("CustomerOrderTestApp Service: Actor rejected {0}: {1}", customerOrder, ex));
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("CustomerOrderTestApp: Exception {0}: {1}", customerOrder, ex));
                throw;
            }

            return orderId;
        }

        public static async Task GetOrderStatus(Guid customerOrderId)
        {
            var customerOrder = ActorProxy.Create<ICustomerOrderActor>(new ActorId(customerOrderId), applicationName);
            var status = await customerOrder.GetStatusAsync();
            Console.WriteLine("Order status is: " + status);
            return;
        }

        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var endpoint = args[0];
                Console.WriteLine("Conencting to cluster: " + endpoint);
                var resolver = new ServicePartitionResolver(endpoint);
                ServicePartitionResolver.SetDefault(resolver);
            }

            RunAsync().Wait();

            Console.ReadLine();
        }

        private static async Task RunAsync()
        {
            //Get a customer view of data
            //From that view, create an order of a certain # of objects, and pass this in as a call to inventory service. 
            //Tests both of these functionalities at once. 


            var inventoryServiceClient = ServiceProxy.Create<IInventoryService>(0, InventoryServiceName);
            var storeview = await inventoryServiceClient.GetCustomerInventoryAsync();
            Console.WriteLine("We are printing the storeview from the GetCustomerInventory call through the InventoryService proxy");
            foreach (var view in storeview)
            {
                Console.WriteLine(
                    "This is item {0} with price {1}, description {2}, and customer available stock of {3} ",
                    view.Id.ToString(),
                    view.Price.ToString(),
                    view.Description,
                    view.CustomerAvailableStock.ToString());
            }


            var order = createTestOrder(storeview);

            //Part II: Now we test order fulfillment. 
            Console.WriteLine("Now beginning test of customer checkout order");
            var x = await CheckoutTest(order);
            //Try a loop with sleep here

            await GetOrderStatus(x);
            Console.WriteLine("RunAsync: Sleeping for 10 seconds while Order Status is updated.");
            System.Threading.Thread.Sleep(10000);
            await GetOrderStatus(x);
            Console.WriteLine("RunAsync: Sleeping for 10 seconds while Order Status is updated.");
            System.Threading.Thread.Sleep(10000);
            await GetOrderStatus(x);
        }

        private static List<CustomerOrderItem> createTestOrder(IEnumerable<InventoryItemView> store)
        {
            Console.WriteLine("We are creating a test order for the customer now...");
            var order = new List<CustomerOrderItem>();
            foreach (var item in store)
            {
                var quantityToAdd = item.CustomerAvailableStock/10;
                Console.WriteLine(string.Format("We are adding {0} items of item no {1} to customer's order", quantityToAdd.ToString(), item.Id.ToString()));

                var toAdd = new CustomerOrderItem(item.Id, quantityToAdd);
                order.Add(toAdd);
            }
            //Printing contents of list to console
            Console.WriteLine("Printing contents of order list...");
            foreach (var item in order)
            {
                Console.WriteLine(string.Format("Ordering {0} items of type {1}", item.Quantity.ToString(), item.ItemId.ToString()));
            }

            return order;
        }
    }
}