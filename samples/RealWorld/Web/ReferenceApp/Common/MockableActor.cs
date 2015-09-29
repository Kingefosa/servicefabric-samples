// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

namespace Common
{
    using System;
    using System.Threading.Tasks;
    using Common.Wrappers;
    using Microsoft.ServiceFabric.Actors;

    public class MockableActor<T> : Actor<T> where T : class
    {
        public new IStatefulActorHostWrapper Host { get; set; }

        protected new Task<IActorReminder> RegisterReminder(
            string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period, ActorReminderAttributes attribute)
        {
            return this.Host.RegisterReminder(reminderName, state, dueTime, period, attribute);
        }

        protected new Task UnregisterReminder(IActorReminder reminder)
        {
            return this.Host.UnregisterReminder(reminder);
        }

        internal Task<IActorReminder> RegisterReminderAccessor(
            string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period, ActorReminderAttributes attribute)
        {
            return base.RegisterReminder(reminderName, state, dueTime, period, attribute);
        }

        internal Task UnregisterReminderAccessor(IActorReminder reminder)
        {
            return base.UnregisterReminder(reminder);
        }
    }
}