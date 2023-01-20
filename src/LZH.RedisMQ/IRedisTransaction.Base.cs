// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Transport;

namespace LZH.RedisMQ;

public abstract class RedisTransactionBase : IRedisTransaction
{
    private readonly ConcurrentQueue<Message> _bufferList;
    private readonly IMessageSender _messageSender;

    protected RedisTransactionBase(IMessageSender messageSender)
    {
        _messageSender = messageSender;
        _bufferList = new ConcurrentQueue<Message>();
    }

    public bool AutoCommit { get; set; }

    public virtual object? DbTransaction { get; set; }

    public abstract void Commit();

    public abstract Task CommitAsync(CancellationToken cancellationToken = default);

    public abstract void Rollback();

    public abstract Task RollbackAsync(CancellationToken cancellationToken = default);

    public abstract void Dispose();

    protected internal virtual void AddToSent(Message msg)
    {
        _bufferList.Enqueue(msg);
    }

    public virtual void Flush()
    {
        while (!_bufferList.IsEmpty)
        {
            // TODO 这里有可能会失败 目前通过重试+告警处理 重试次数固定 待优化
            if (_bufferList.TryDequeue(out var message))
            {
                _messageSender.SendAsync(message);
            }
        }
    }
}