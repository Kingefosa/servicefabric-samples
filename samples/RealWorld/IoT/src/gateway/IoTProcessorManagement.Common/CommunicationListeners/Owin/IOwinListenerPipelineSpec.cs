// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
{
    /// <summary>
    ///  defines an Owin Listener specification
    /// CreateOwinPipeline method is expected to create the Owin Pipeline
    /// </summary>
    public interface IOwinListenerSpec
    {
        void CreateOwinPipeline(IAppBuilder app);
    }
}
