// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace RestockRequest.Domain
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public sealed class RestockRequest
    {
        public RestockRequest(Guid itemId, int quantity)
        {
            this.ItemId = itemId;
            this.Quantity = quantity;
        }

        [DataMember]
        public Guid ItemId { get; private set; }

        [DataMember]
        public int Quantity { get; private set; }

        public override string ToString() => $"{this.ItemId}[Quantity = {this.Quantity}]";
    }
}