// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using RedisMQ.Internal;
using RedisMQ.Messages;

namespace RedisMQ.Transport
{
    public interface IDispatcher : IProcessingServer
    {

        ValueTask Execute(Message message, ConsumerExecutorDescriptor? descriptor = null);

        
    }
}