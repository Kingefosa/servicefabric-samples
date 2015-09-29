// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Web.Service.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Common;
    using Inventory.Domain;
    using Microsoft.ServiceFabric.Services;

    public class StoreController : ApiController
    {
        public const string InventoryServiceName = "InventoryService";

        /// <summary>
        /// Right now, this method makes an API call via a ServiceProxy to retrieve Inventory Data directly
        /// from InventoryService. In the future, this call will be made with a specified category parameter, 
        /// and based on this could call a specific materialized view to return. There would be no option 
        /// to return the entire inventory service in one call, as this would be slow and expensive at scale.  
        /// </summary>
        /// <returns>Task of type IEnumerable of InventoryItemView objects</returns>
        [HttpGet]
        [Route("api/store")]
        public Task<IEnumerable<InventoryItemView>> GetStore()
        {
            var builder = new ServiceUriBuilder(InventoryServiceName);

            var inventoryServiceClient = ServiceProxy.Create<IInventoryService>(0, builder.ToUri());
            //Right now, even if your service has no partitions, you should specify a partion key as 0 in your Create call to a Service Proxy interface.
            return inventoryServiceClient.GetCustomerInventoryAsync();
        }
    }
}