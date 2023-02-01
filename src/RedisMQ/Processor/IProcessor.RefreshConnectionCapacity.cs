// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisMQ.RedisStream;

namespace RedisMQ.Processor;

internal class RefreshConnectionCapacityProcessor : IProcessor
{
    private readonly ILogger<RefreshConnectionCapacityProcessor> _logger;
    private readonly TimeSpan _waitingInterval;
    private readonly IRedisConnectionPool _connectionPool;

    public RefreshConnectionCapacityProcessor(ILogger<RefreshConnectionCapacityProcessor> logger,
        IRedisConnectionPool connectionPool)
    {
        _logger = logger;
        _connectionPool = connectionPool;
        _waitingInterval = TimeSpan.FromSeconds(3);
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        while (true)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.ThrowIfStopping();

            _logger.LogDebug("refresh connection capacity...");

            _connectionPool.RefreshConnectionCapacity();
        
            _logger.LogDebug("refresh connection capacity success!");

            await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
        }
     
    }
}