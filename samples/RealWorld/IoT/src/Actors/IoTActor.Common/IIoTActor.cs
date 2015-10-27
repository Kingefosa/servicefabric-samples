// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace IoTActor.Common
{
    public interface IIoTActor : IActor
    {
        Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body);
    }
}
