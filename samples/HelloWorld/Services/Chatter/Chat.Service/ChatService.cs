// -----------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chat.Service
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Domain;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services;

    public class ChatService : StatefulService, IChatService
    {
        private IReliableDictionary<DateTimeOffset, Message> messageDictionary;
        private readonly int MessagesToKeep = 5;
        private readonly TimeSpan MessageLifetime = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Saves the given message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task AddMessageAsync(Message message)
        {
            IReliableDictionary<DateTimeOffset, Message> messagesDictionary = await this.GetMessageDictionaryAsync();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await messagesDictionary.AddAsync(tx, DateTimeOffset.UtcNow, message);
                await tx.CommitAsync();
            }
        }

        /// <summary>
        /// Gets all messages.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessages()
        {
            IReliableDictionary<DateTimeOffset, Message> messagesDictionary = await this.GetMessageDictionaryAsync();
            return this.messageDictionary.CreateEnumerable(EnumerationMode.Ordered);
        }

        /// <summary>
        /// Removes all messages.
        /// </summary>
        /// <returns></returns>
        public async Task ClearMessagesAsync()
        {
            IReliableDictionary<DateTimeOffset, Message> messagesDictionary = await this.GetMessageDictionaryAsync();
            await messagesDictionary.ClearAsync();
        }

        /// <summary>
        /// Gets the message dictionary and caches the result.
        /// </summary>
        /// <returns></returns>
        protected async Task<IReliableDictionary<DateTimeOffset, Message>> GetMessageDictionaryAsync()
        {
            if (this.messageDictionary == null)
            {
                this.messageDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<DateTimeOffset, Message>>("messages");
            }
            return this.messageDictionary;
        }

        /// <summary>
        /// Creates a set of listeners that open up endpoints for clients and other services to use to talk to this service.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(parameters => new ServiceCommunicationListener<IChatService>(parameters, this)) };
        }

        /// <summary>
        /// Executes a continuous loop that trims messages that are too old.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "Chat service started processing messages.");

            IReliableDictionary<DateTimeOffset, Message> messageDictionary = await this.GetMessageDictionaryAsync();
            //Use this method to periodically clean up messages in the messagesDictionary
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Remove all the messages that are older than x seconds keeping the last y messages
                    IEnumerable<KeyValuePair<DateTimeOffset, Message>> oldMessages = 
                        from t in this.messageDictionary
                        where t.Key < (DateTimeOffset.UtcNow - MessageLifetime) orderby t.Key ascending 
                        select t;

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        foreach (KeyValuePair<DateTimeOffset, Message> item in oldMessages.Take(this.messageDictionary.Count() - MessagesToKeep))
                        {
                            await this.messageDictionary.TryRemoveAsync(tx, item.Key);
                        }

                        await tx.CommitAsync();
                    }
                    
                }
                catch (Exception e)
                {
                    if (!this.ContinueOnException(e))
                    {
                        ServiceEventSource.Current.ServiceMessage(
                            this,
                            "Message processing stopped because of error {0}",
                            e);

                        return;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        private bool ContinueOnException(Exception e)
        {
            {
                if ((e is FabricNotPrimaryException) || // replica is no longer writable
                    (e is FabricObjectClosedException) || // replica is closed
                    (e is FabricNotReadableException)) // replica is not readable
                {
                    return false;
                }
                if (e is TimeoutException)
                {
                    // Service Fabric uses timeouts on collection operations to prevent deadlocks.
                    // If this exception is thrown, it means that this transaction was waiting the default
                    // amount of time (4 seconds) but was unable to acquire the lock. In this case we simply
                    // retry after a random backoff interval. You can also control the timeout via a parameter
                    // on the collection operation.
                    return true;
                }

                return false;
            }
        }
    }
}