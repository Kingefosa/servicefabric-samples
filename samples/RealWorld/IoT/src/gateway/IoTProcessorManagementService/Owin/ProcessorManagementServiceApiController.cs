// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagementService
{
    //Api Controller implement this interface, 
    // Owin creates a dependancy resolver that assign it to each controller.
    public interface ProcessorManagementServiceApiController
    {
        ProcessorManagementService Svc { get; set; }
    }
}
