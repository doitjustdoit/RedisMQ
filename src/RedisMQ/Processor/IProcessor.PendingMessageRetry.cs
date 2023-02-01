// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RedisMQ.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisMQ.Messages;
using RedisMQ.RedisStream;
using RedisMQ.Transport;

namespace RedisMQ.Processor;

public class ScanFailedMessageProcessor : IProcessor
{
    private readonly ILogger<ScanFailedMessageProcessor> _logger;
    private readonly TimeSpan _waitingInterval;
    private ConcurrentDictionary<string, IReadOnlyList<ConsumerExecutorDescriptor>> _groupingMatches;
    private readonly IServiceProvider _serviceProvider;
    private readonly MethodMatcherCache _selector;
    private readonly IRedisStreamManager _redisStreamManager;
    private readonly IConsumerClientFactory _consumerClientFactory;
    /// <summary>
    /// topic - group
    /// </summary>
    private List<Tuple<string, string>> _streamInfos;

    private readonly IOptions<RedisMQOptions> _options;

    public ScanFailedMessageProcessor(ILogger<ScanFailedMessageProcessor> logger,IOptions<RedisMQOptions> options,
        IConsumerClientFactory consumerClientFactory, IServiceProvider serviceProvider,IRedisStreamManager redisStreamManager)
    {
        _options = options;
        _consumerClientFactory = consumerClientFactory;
        _redisStreamManager = redisStreamManager;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _waitingInterval = TimeSpan.FromSeconds(3);
        _selector = _serviceProvider.GetRequiredService<MethodMatcherCache>();
    }

    public virtual async Task ProcessAsync(ProcessingContext context)
    {
        if (_groupingMatches is null)
        {
            Init();
        }
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.ThrowIfStopping();

        ScanFailedMessages(context.CancellationToken);
       
        _logger.LogDebug("Transport connection checking...");

        await context.WaitAsync(_waitingInterval).ConfigureAwait(false);
    }

    private async Task ScanFailedMessages(CancellationToken cancellationToken)
    {
        List<TransportMessage> notifyMsgs = new();
        foreach (var streamInfo in _streamInfos)
        {
            var failedMessagesId = await _redisStreamManager.PollStreamsFailedMessagesIdAsync(streamInfo.Item1, streamInfo.Item2,cancellationToken);
            foreach (var msgId in failedMessagesId)
            {
                var msgEntry=await _redisStreamManager.PollStreamsCertainMessageAsync(streamInfo.Item1, streamInfo.Item2, msgId,
                    cancellationToken);
                TransportMessage msg= RedisMessage.Create(msgEntry, streamInfo.Item2);
                notifyMsgs.Add(msg);
                await _redisStreamManager.TransferFailedMessageToDeadLetterAsync(msg, msgId);
            }
           
        }
        Parallel.ForEach(notifyMsgs,(msg) =>
        {
            _options.Value.FailedThresholdCallback?.Invoke(msg);
        });
    }

    private void Init()
    {
        _groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();
        _streamInfos = new();
        foreach ( var matchGroup in _groupingMatches)
        {
            using var client = _consumerClientFactory.Create(matchGroup.Key);
            var topics = client.FetchTopics(matchGroup.Value.Select(x =>  x.TopicName));
            _streamInfos.AddRange(topics.Select(it=>new Tuple<string, string>(it,matchGroup.Key)));
        }
    }
}