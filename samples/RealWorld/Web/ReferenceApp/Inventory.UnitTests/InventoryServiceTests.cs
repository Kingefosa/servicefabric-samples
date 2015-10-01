// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Inventory.UnitTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Inventory.Domain;
    using Inventory.Service;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mocks;

    [TestClass]
    public class InventoryServiceTests
    {
        [TestMethod]
        public async Task TestCreateAndIsItemInInventoryAsync()
        {
            MockReliableStateManager stateManager = new MockReliableStateManager();
            InventoryService target = new InventoryService(stateManager);

            InventoryItem expected = new InventoryItem("test", 1, 10, 1, 10);

            await target.CreateInventoryItemAsync(expected);
            bool resultTrue = await target.IsItemInInventoryAsync(expected.Id);
            bool resultFalse = await target.IsItemInInventoryAsync(Guid.NewGuid());

            Assert.IsTrue(resultTrue);
            Assert.IsFalse(resultFalse);
        }

        [TestMethod]
        public async Task TestAddStock()
        {
            int expectedQuantity = 10;
            int quantityToAdd = 3;
            MockReliableStateManager stateManager = new MockReliableStateManager();
            InventoryService target = new InventoryService(stateManager);

            InventoryItem item = new InventoryItem("test", 1, expectedQuantity - quantityToAdd, 1, expectedQuantity);

            RestockRequest.Domain.RestockRequest request = new RestockRequest.Domain.RestockRequest(item.Id, quantityToAdd);

            await target.CreateInventoryItemAsync(item);
            int actualAdded = await target.AddStockAsync(Enumerable.Repeat(request, 1));

            Assert.AreEqual(quantityToAdd, actualAdded);
            Assert.AreEqual(item.AvailableStock, expectedQuantity);
        }

        [TestMethod]
        public async Task TestRemoveStock()
        {
            int expectedQuantity = 5;
            int quantityToRemove = 3;
            MockReliableStateManager stateManager = new MockReliableStateManager();
            InventoryService target = new InventoryService(stateManager);

            InventoryItem item = new InventoryItem("test", 1, expectedQuantity + quantityToRemove, 1, expectedQuantity);

            await target.CreateInventoryItemAsync(item);
            int actualRemoved = await target.RemoveStockAsync(item.Id, quantityToRemove);

            Assert.AreEqual(quantityToRemove, actualRemoved);
            Assert.AreEqual(expectedQuantity, item.AvailableStock);
        }
    }
}