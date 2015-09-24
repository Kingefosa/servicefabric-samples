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
        Task RestockRequestsCompleted(IList<RestockRequest> requests);

        //Called by Customer Order Actor to remove stock when it is cycling through. 

        Task<int> RemoveStock(Guid itemId, int quantity);

        Task<bool> IsItemInInventoryAsync(Guid itemId);

        //Called by Customer Storeview to get the customer view of inventory.
        Task<IEnumerable<InventoryItemView>> GetCustomerInventoryAsync();

        // TODO: should this exposed through proxy?
        // Or can we create the inventory items through a config file?
        Task CreateInventoryItemAsync(Guid itemId, string description, decimal price, int availableStock, int restockThreshold, int maxProductionThreshold);

        //Used in testing
        Task CheckCustomerView(IEnumerable<InventoryItemView> custView);
    }
}