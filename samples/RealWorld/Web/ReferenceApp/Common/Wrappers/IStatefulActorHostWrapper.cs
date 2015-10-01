// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Wrappers
{
    using System;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// Interface to wrap the read-only StatefulActorHost methods so we can inject custom implementations into actors.
    /// This way we can inject a mock for unit testing.
    /// </summary>
    public interface IStatefulActorHostWrapper
    {
        IStatefulServicePartition Partition { get; }

        StatefulServiceInitializationParameters StatefulServiceInitializationParameters { get; }

        IActorStateProvider StateProvider { get; }

        Task<IActorReminder> RegisterReminder(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period, ActorReminderAttributes attribute);
        Task UnregisterReminder(IActorReminder reminder);
    }
}