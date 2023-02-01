// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RedisMQ.Insternal;
using RedisMQ.Internal;
using RedisMQ.Messages;
using RedisMQ.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RedisMQ.Processor;

public class Dispatcher : IDispatcher
{
    private CancellationTokenSource? _tasksCts;
    private readonly CancellationTokenSource _delayCts = new();
    private readonly ISubscribeExecutor _executor;
    private readonly ILogger<Dispatcher> _logger;
    private readonly RedisMQOptions _options;

    private readonly IConsumerClientFactory _consumerClientFactory;

    public Dispatcher(ILogger<Dispatcher> logger,
        IOptions<RedisMQOptions> options,
        ISubscribeExecutor executor,
            IConsumerClientFactory consumerClientFactory)
    {
        _consumerClientFactory=consumerClientFactory;
        _logger = logger;
        _options = options.Value;
        _executor = executor;
    }

    public void Start(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        _tasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);
        _tasksCts.Token.Register(() => _delayCts.Cancel());
     
        _logger.LogInformation("Starting default Dispatcher");
    }

    public async ValueTask Execute(Message message, ConsumerExecutorDescriptor? descriptor = null)
    {
        try
        {
            var res=await _executor.ExecuteAsync(message, descriptor, _tasksCts!.Token).ConfigureAwait(false);
            if (res.Succeeded)
            {
                using var _consumerClient = _consumerClientFactory.Create(message.Headers[Headers.Group]);
                _consumerClient.Commit((message.Headers[Headers.MessageName],message.Headers[Headers.Group],message.Headers[Headers.StreamMessageId]));
            }
            else
            {
                _logger.LogError(res.Exception,"An exception occurred when invoke subscriber.");
            }
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"An exception occurred when invoke subscriber. MessageId:{message.GetId()}");
        }
    }

    public void Dispose()
    {
        _tasksCts?.Dispose();
    }
}