// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using RedisMQ.Messages;

namespace RedisMQ.Internal;

/// <summary>
/// Consumer executor
/// </summary>
public interface ISubscribeExecutor
{
    Task<OperateResult> ExecuteAsync(Message message, ConsumerExecutorDescriptor? descriptor = null, CancellationToken cancellationToken = default);
}