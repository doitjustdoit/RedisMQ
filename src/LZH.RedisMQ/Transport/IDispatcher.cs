// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Messages;

namespace LZH.RedisMQ.Transport
{
    public interface IDispatcher : IProcessingServer
    {
        void EnqueueToPublish(Message message);

        void EnqueueToExecute(Message message, ConsumerExecutorDescriptor descriptor);
    }
}