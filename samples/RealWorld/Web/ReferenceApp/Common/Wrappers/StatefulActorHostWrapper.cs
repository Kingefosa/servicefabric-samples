// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Common.Wrappers
{
    using System;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    public class StatefulActorHostWrapper<T> : IStatefulActorHostWrapper where T : class
    {
        private readonly MockableActor<T> actor;

        public StatefulActorHostWrapper(MockableActor<T> actor)
        {
            this.actor = actor;
        }

        public IStatefulServicePartition Partition
        {
            get { return this.actor.Host.Partition; }
        }

        public StatefulServiceInitializationParameters StatefulServiceInitializationParameters
        {
            get { return this.actor.Host.StatefulServiceInitializationParameters; }
        }

        public IActorStateProvider StateProvider
        {
            get { return this.actor.Host.StateProvider; }
        }

        public Task<IActorReminder> RegisterReminder(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period, ActorReminderAttributes attribute)
        {
            return this.actor.RegisterReminderAccessor(reminderName, state, dueTime, period, attribute);
        }

        public Task UnregisterReminder(IActorReminder reminder)
        {
            return this.actor.UnregisterReminderAccessor(reminder);
        }
    }
}