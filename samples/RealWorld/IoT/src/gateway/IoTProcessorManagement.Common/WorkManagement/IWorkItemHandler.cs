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
    public interface IWorkItemHandler<Wi> where Wi : IWorkItem 
    {
        Task<Wi> HandleWorkItem(Wi workItem);
    }
}
