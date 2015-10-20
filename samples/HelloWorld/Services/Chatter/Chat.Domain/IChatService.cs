// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chat.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services;

    public interface IChatService : IService
    {
        Task AddMessageAsync(Message message);
        Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessages();
        Task ClearMessagesAsync();
    }
}