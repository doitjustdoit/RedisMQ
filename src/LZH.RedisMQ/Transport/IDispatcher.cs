// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Messages;

namespace LZH.RedisMQ.Transport
{
    public interface IDispatcher : IProcessingServer
    {
        ValueTask EnqueueToPublish(Message message);

        ValueTask EnqueueToExecute(Message message, ConsumerExecutorDescriptor? descriptor = null);

        ValueTask EnqueueToScheduler(Message message, DateTime publishTime, object? transaction = null);
        
    }
}