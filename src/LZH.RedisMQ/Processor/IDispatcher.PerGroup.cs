// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LZH.RedisMQ.Insternal;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LZH.RedisMQ.Processor;

internal class DispatcherPerGroup : IDispatcher
{
    private CancellationTokenSource? _tasksCts;
    private readonly CancellationTokenSource _delayCts = new();
    private readonly ISubscribeExecutor _executor;
    private readonly ILogger<Dispatcher> _logger;
    private readonly RedisMQOptions _options;
    private readonly IMessageSender _sender;
    private readonly Queue<Message> _schedulerQueue;

    private Channel<Message> _publishedChannel = default!;
    private ConcurrentDictionary<string, Channel<(Message, ConsumerExecutorDescriptor?)>> _receivedChannels = default!;

    private DateTime _nextSendTime = DateTime.MaxValue;

    public DispatcherPerGroup(ILogger<Dispatcher> logger,
        IMessageSender sender,
        IOptions<RedisMQOptions> options,
        ISubscribeExecutor executor
        )
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _executor = executor;
        _schedulerQueue = new ();
    }

    public async void Start(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        _tasksCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, CancellationToken.None);
        _tasksCts.Token.Register(() => _delayCts.Cancel());

        var capacity = _options.ProducerThreadCount * 500;
        _publishedChannel = Channel.CreateBounded<Message>(
            new BoundedChannelOptions(capacity > 5000 ? 5000 : capacity)
            {
                AllowSynchronousContinuations = true,
                SingleReader = _options.ProducerThreadCount == 1,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        await Task.WhenAll(Enumerable.Range(0, _options.ProducerThreadCount)
            .Select(_ => Task.Factory.StartNew(Sending, _tasksCts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

        _receivedChannels =
            new ConcurrentDictionary<string, Channel<(Message, ConsumerExecutorDescriptor?)>>(
                _options.ConsumerThreadCount, _options.ConsumerThreadCount * 2);

        GetOrCreateReceiverChannel(_options.DefaultGroupName);

        _ = Task.Factory.StartNew(async () =>
        {
            //When canceling, place the message status of unsent in the queue to delayed
            _tasksCts.Token.Register(() =>
            {
                try
                {
                    if (_schedulerQueue.Count == 0) return;

                    var messageIds = _schedulerQueue.Select(x => x.GetId()).ToArray();
                    _logger.LogDebug("Update storage to delayed success of delayed message in memory queue!");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Update storage fails of delayed message in memory queue!");
                }
            });

            while (!_tasksCts.Token.IsCancellationRequested)
            {
                try
                {
                    while (_schedulerQueue.TryPeek(out var msg))
                    {
                        var delayTime = _nextSendTime - DateTime.Now;

                        if (delayTime > new TimeSpan(500000)) //50ms
                        {
                            await Task.Delay(delayTime, _delayCts.Token);
                        }
                        _tasksCts.Token.ThrowIfCancellationRequested();

                        await _sender.SendAsync(_schedulerQueue.Dequeue()).ConfigureAwait(false);
                    }
                    _tasksCts.Token.WaitHandle.WaitOne(100);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }, _tasksCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);

        _logger.LogInformation("Starting DispatcherPerGroup");
    }

    public async ValueTask EnqueueToScheduler(Message message, DateTime publishTime, object? transaction = null)
    {
        var timeSpan = publishTime - DateTime.Now;

        if (timeSpan <= TimeSpan.FromMinutes(1))
        {
            _schedulerQueue.Enqueue(message);

            if (publishTime < _nextSendTime)
            {
                _delayCts.Cancel();
            }
        }
        else
        {
        }
    }

    public async ValueTask EnqueueToPublish(Message message)
    {
        try
        {
            if (!_publishedChannel.Writer.TryWrite(message))
                while (await _publishedChannel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
                    if (_publishedChannel.Writer.TryWrite(message))
                        return;
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
    }

    public async ValueTask EnqueueToExecute(Message message, ConsumerExecutorDescriptor? descriptor)
    {
        try
        {
            var group = message.GetGroup()!;

            var channel = GetOrCreateReceiverChannel(group);

            if (!channel.Writer.TryWrite((message, descriptor)))
                while (await channel.Writer.WaitToWriteAsync(_tasksCts!.Token).ConfigureAwait(false))
                    if (channel.Writer.TryWrite((message, descriptor)))
                        return;
        }
        catch (OperationCanceledException)
        {
            //Ignore
        }
    }

    public void Dispose()
    {
        _tasksCts?.Dispose();
    }

    private Channel<(Message, ConsumerExecutorDescriptor?)> GetOrCreateReceiverChannel(string key)
    {
        return _receivedChannels.GetOrAdd(key, group =>
        {
            _logger.LogInformation(
                "Creating receiver channel for group {ConsumerGroup} with thread count {ConsumerThreadCount}", group,
                _options.ConsumerThreadCount);

            var capacity = _options.ConsumerThreadCount * 300;
            var channel = Channel.CreateBounded<(Message, ConsumerExecutorDescriptor?)>(
                new BoundedChannelOptions(capacity > 3000 ? 3000 : capacity)
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = _options.ConsumerThreadCount == 1,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            Task.WhenAll(Enumerable.Range(0, _options.ConsumerThreadCount)
                .Select(_ => Task.Factory.StartNew(() => Processing(group, channel), _tasksCts!.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            return channel;
        });
    }

    private async Task Sending()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
                while (_publishedChannel.Reader.TryRead(out var message))
                    try
                    {
                        var result = await _sender.SendAsync(message).ConfigureAwait(false);
                        if (!result.Succeeded)
                            _logger.MessagePublishException(
                                message.GetId(),
                                result.ToString(),
                                result.Exception);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An exception occurred when sending a message to the MQ. Id:{message.GetId()}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    private async Task Processing(string group, Channel<(Message, ConsumerExecutorDescriptor?)> channel)
    {
        try
        {
            while (await channel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
                while (channel.Reader.TryRead(out var message))
                    try
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Dispatching message for group {ConsumerGroup}", group);

                        await _executor.ExecuteAsync(message.Item1, message.Item2, _tasksCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        //expected
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                            $"An exception occurred when invoke subscriber. MessageId:{message.Item1.GetId()}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }
}