// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using LZH.RedisMQ.Messages;

namespace LZH.RedisMQ.Internal
{
    /// <summary>
    /// Consumer executor
    /// </summary>
    public interface ISubscribeDispatcher
    {
        Task<OperateResult> DispatchAsync(Message message, CancellationToken cancellationToken = default);

        Task<OperateResult> DispatchAsync(Message message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken = default);
    }
}