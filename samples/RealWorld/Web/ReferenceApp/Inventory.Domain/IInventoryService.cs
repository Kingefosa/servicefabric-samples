// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Inventory.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services;
    using RestockRequest.Domain;

    public interface IInventoryService : IService
    {
        Task<int> AddStockAsync(IEnumerable<RestockRequest> requests);
        Task<int> RemoveStockAsync(Guid itemId, int quantity);
        Task<bool> IsItemInInventoryAsync(Guid itemId);
        Task<IEnumerable<InventoryItemView>> GetCustomerInventoryAsync();
        Task CreateInventoryItemAsync(InventoryItem item);
    }
}