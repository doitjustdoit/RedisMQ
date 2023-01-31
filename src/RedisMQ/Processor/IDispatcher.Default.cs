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
    private readonly IMessageSender _sender;
    private readonly Queue<Message> _schedulerQueue;

    private Channel<Message> _publishedChannel = default!;
    private long _nextSendTime = DateTime.MaxValue.Ticks;
    private readonly IConsumerClientFactory _consumerClientFactory;

    public Dispatcher(ILogger<Dispatcher> logger,
        IMessageSender sender,
        IOptions<RedisMQOptions> options,
        ISubscribeExecutor executor,
            IConsumerClientFactory consumerClientFactory)
    {
        _consumerClientFactory=consumerClientFactory;
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
                TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray()).ConfigureAwait(false);

       
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
                        var delayTime = _nextSendTime - DateTime.Now.Ticks;

                        if (delayTime > 500000) //50ms
                        {
                            await Task.Delay(new TimeSpan(delayTime), _delayCts.Token);
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

        _logger.LogInformation("Starting default Dispatcher");
    }

    public async ValueTask EnqueueToScheduler(Message message, DateTime publishTime, object? transaction = null)
    {
        var timeSpan = publishTime - DateTime.Now;

        if (timeSpan <= TimeSpan.FromMinutes(1)) //1min
        {
            _schedulerQueue.Enqueue(message);

            if (publishTime.Ticks < _nextSendTime)
            {
                _delayCts.Cancel();
            }
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

    public async ValueTask EnqueueToExecute(Message message, ConsumerExecutorDescriptor? descriptor = null)
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

    private async ValueTask Sending()
    {
        try
        {
            while (await _publishedChannel.Reader.WaitToReadAsync(_tasksCts!.Token).ConfigureAwait(false))
                while (_publishedChannel.Reader.TryRead(out var message))
                    try
                    {
                        var result = await _sender.SendAsync(message).ConfigureAwait(false);
                        if (!result.Succeeded)
                            _logger.MessagePublishException(message.GetId(), result.ToString(), result.Exception);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"An exception occurred when sending a message to the MQ. Id:{message.GetId()}");
                    }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

}