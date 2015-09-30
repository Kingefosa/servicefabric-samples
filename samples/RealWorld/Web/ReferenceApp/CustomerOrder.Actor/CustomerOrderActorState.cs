// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace CustomerOrder.Actor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using CustomerOrder.Domain;

    [DataContract]
    internal sealed class CustomerOrderActorState
    {
        [DataMember]
        public IList<CustomerOrderItem> OrderedItems { get; set; }

        [DataMember]
        public IDictionary<Guid, int> FulfilledItems { get; set; }

        [DataMember]
        public IList<Guid> BackorderedItems { get; set; }

        [DataMember]
        public CustomerOrderStatus Status { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Status: " + this.Status + ".");
            if (this.OrderedItems != null)
            {
                sb.Append("Ordered Items: ");
                sb.Append(String.Join(",", this.OrderedItems.Select<CustomerOrderItem, string>(item => item.ItemId + "-" + item.Quantity)));
                sb.AppendLine();
            }
            if (this.FulfilledItems != null)
            {
                sb.Append("Fulfilled Items: ");
                foreach (KeyValuePair<Guid, int> kvp in this.FulfilledItems)
                {
                    sb.Append("Item Id" + kvp.Key + ", Quantity: " + kvp.Value + ',');
                }
                sb.AppendLine();
            }
            if (this.BackorderedItems != null)
            {
                sb.Append("Backordered Items: ");
                sb.Append(String.Join(",", this.BackorderedItems));
            }

            return sb.ToString();
        }
    }
}