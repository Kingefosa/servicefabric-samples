﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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