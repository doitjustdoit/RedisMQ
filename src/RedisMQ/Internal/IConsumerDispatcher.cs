﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace RedisMQ.Internal
{
    /// <summary>
    /// Handler received message of subscribed.
    /// </summary>
    public interface IConsumerDispatcher : IProcessingServer
    {
        bool IsHealthy();

        void ReStart(bool force = false);
        
        
    }
}