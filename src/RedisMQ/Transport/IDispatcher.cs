// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisMQ.Internal;
using RedisMQ.Messages;

namespace RedisMQ.Transport
{
    public interface IDispatcher : IProcessingServer
    {

        ValueTask EnqueueToExecute(Message message, ConsumerExecutorDescriptor? descriptor = null);

        ValueTask EnqueueToScheduler(Message message, DateTime publishTime, object? transaction = null);
        
    }
}