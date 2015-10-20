// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chat.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Domain;
    using Microsoft.ServiceFabric.Services;

    /// <summary>
    /// This controller accepts WebApi calls from the index.html when the AJAX calls are made
    /// </summary>
    [Route("api/chat")]
    public class ChatController : ApiController
    {
        // The service only has a single partition. In order to access the partition need to provide a
        // value in the LowKey - HighKey range defined in the ApplicationManifest.xml
        private long defaultPartitionID = 1;
        private Uri chatServiceInstance = new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/ChatService");

        /// <summary>
        /// GET: api/chat
        /// </summary>
        [HttpGet]
        public Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessages()
        {
            IChatService proxy = ServiceProxy.Create<IChatService>(this.defaultPartitionID, chatServiceInstance);
            return proxy.GetMessages();
        }

        /// <summary>
        /// POST api/chat
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        public Task Add([FromBody] Message message)
        {
            if (message == null)
            {
                ServiceEventSource.Current.Message("Received message with no content.");
                throw new HttpResponseException(System.Net.HttpStatusCode.BadRequest);
            }

            IChatService proxy = ServiceProxy.Create<IChatService>(this.defaultPartitionID, chatServiceInstance);
            return proxy.AddMessageAsync(message);
        }

        /// <summary>
        /// DELETE api/chat
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public Task ClearMessages()
        {
            IChatService proxy = ServiceProxy.Create<IChatService>(this.defaultPartitionID, chatServiceInstance);
            return proxy.ClearMessagesAsync();
        }
    }
}