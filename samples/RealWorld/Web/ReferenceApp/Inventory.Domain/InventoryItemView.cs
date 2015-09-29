// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Inventory.Domain
{
    using System;
    using System.Runtime.Serialization;

    //Guid will always be key to this value pair
    [DataContract]
    public sealed class InventoryItemView
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public int CustomerAvailableStock { get; set; }
    }
}