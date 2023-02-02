// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using RedisMQ.Internal;
using Microsoft.Extensions.Logging;

namespace RedisMQ.Processor;

public class TransportCheckProcessor : IProcessor
{
    private readonly ILogger<TransportCheckProcessor> _logger;
    private readonly IConsumerDispatcher _dispatcher;
    private readonly TimeSpan _waitingInterval;

    public TransportCheckProcessor(ILogger<TransportCheckProcessor> logger, IConsumerDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _waitingInterval = TimeSpan.FromSeconds(30);
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        while (true)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.ThrowIfStopping();

            _logger.LogDebug("Transport connection checking...");

            if (!_dispatcher.IsHealthy())
            {
                _logger.LogWarning("Transport connection is unhealthy, reconnection...");

                _dispatcher.ReStart();
            }
            else
            {
                _logger.LogDebug("Transport connection healthy!");
            }

            await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
        }
        
    }
}