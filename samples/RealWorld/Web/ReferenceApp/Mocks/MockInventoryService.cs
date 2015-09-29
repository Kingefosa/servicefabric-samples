// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Inventory.Domain;

    public class MockInventoryService : IInventoryService
    {
        public MockInventoryService()
        {
            this.AddStockAsyncFunc = requests => Task.FromResult(requests.Count());
            this.CreateInventoryItemAsyncFunc = item => Task.FromResult(true);
            this.GetCustomerInventoryAsyncFunc = () => Task.FromResult<IEnumerable<InventoryItemView>>(new List<InventoryItemView>() {new InventoryItemView()});
            this.IsItemInInventoryAsyncFunc = (itemId) => Task.FromResult(true);
            this.RemoveStockAsyncFunc = (itemId, quantity) => Task.FromResult(quantity);
        }

        public Func<IEnumerable<RestockRequest.Domain.RestockRequest>, Task<int>> AddStockAsyncFunc { get; set; }

        public Func<InventoryItem, Task> CreateInventoryItemAsyncFunc { get; set; }

        public Func<Task<IEnumerable<InventoryItemView>>> GetCustomerInventoryAsyncFunc { get; set; }

        public Func<Guid, Task<bool>> IsItemInInventoryAsyncFunc { get; set; }

        public Func<Guid, int, Task<int>> RemoveStockAsyncFunc { get; set; }

        public Task<int> AddStockAsync(IEnumerable<RestockRequest.Domain.RestockRequest> requests)
        {
            return this.AddStockAsyncFunc(requests);
        }

        public Task CreateInventoryItemAsync(InventoryItem item)
        {
            return this.CreateInventoryItemAsyncFunc(item);
        }

        public Task<IEnumerable<InventoryItemView>> GetCustomerInventoryAsync()
        {
            return this.GetCustomerInventoryAsyncFunc();
        }

        public Task<bool> IsItemInInventoryAsync(Guid itemId)
        {
            return this.IsItemInInventoryAsyncFunc(itemId);
        }

        public Task<int> RemoveStockAsync(Guid itemId, int quantity)
        {
            return this.RemoveStockAsyncFunc(itemId, quantity);
        }
    }
}