// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace CustomerOrder.Domain
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public sealed class CustomerOrderItem
    {
        public CustomerOrderItem(Guid itemId, int quantity)
        {
            this.ItemId = itemId;
            this.Quantity = quantity;
        }

        [DataMember]
        public Guid ItemId { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }
}