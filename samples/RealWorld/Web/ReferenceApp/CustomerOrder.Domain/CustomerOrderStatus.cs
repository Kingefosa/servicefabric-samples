// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace CustomerOrder.Domain
{
    public enum CustomerOrderStatus
    {
        NA,
        Confirmed,
        Submitted,
        InProcess,
        Backordered,
        Shipped,
        Canceled,
    }
}