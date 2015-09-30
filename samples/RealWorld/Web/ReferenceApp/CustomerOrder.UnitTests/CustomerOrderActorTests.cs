// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace CustomerOrder.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CustomerOrder.Actor;
    using CustomerOrder.Domain;
    using Inventory.Domain;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mocks;

    [TestClass]
    public class CustomerOrderActorTests
    {
        [TestMethod]
        public async Task TestFulfillOrderSimple()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy();
            serviceProxy.Supports<IInventoryService>(serviceUri => new MockInventoryService());

            CustomerOrderActor target = new CustomerOrderActor();
            target.ServiceProxy = new MockServiceProxy();
            target.State.Status = CustomerOrderStatus.Submitted;
            target.State.OrderedItems = new List<CustomerOrderItem>()
            {
                new CustomerOrderItem(Guid.NewGuid(), 4)
            };

            int onBackorder = await target.FulfillOrder();

            Assert.AreEqual<CustomerOrderStatus>(CustomerOrderStatus.Shipped, target.State.Status);
            Assert.AreEqual<int>(0, onBackorder);
        }

        [TestMethod]
        public async Task TestFulfillOrderWithBackorder()
        {
            MockServiceProxy serviceProxy = new MockServiceProxy();
            serviceProxy.Supports<IInventoryService>(
                serviceUri => new MockInventoryService()
                {
                    RemoveStockAsyncFunc = (itemId, quantity) => Task.FromResult(quantity - 1)
                });

            CustomerOrderActor target = new CustomerOrderActor();
            target.ServiceProxy = new MockServiceProxy();
            target.State.Status = CustomerOrderStatus.Submitted;
            target.State.OrderedItems = new List<CustomerOrderItem>()
            {
                new CustomerOrderItem(Guid.NewGuid(), 4)
            };

            int onBackorder = await target.FulfillOrder();

            Assert.AreEqual<CustomerOrderStatus>(CustomerOrderStatus.Shipped, target.State.Status);
            Assert.AreEqual<int>(0, onBackorder);
        }
    }
}