// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    public enum ICommunicationListenerStatus
    {
        Closed,
        Opening,
        Opened,  
        Closing,
        Initializing,
        Initialized,
        Aborting,
        Aborted
    }
}
