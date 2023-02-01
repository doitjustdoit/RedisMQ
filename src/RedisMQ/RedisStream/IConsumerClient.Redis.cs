// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisMQ.Messages;
using RedisMQ.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    internal class RedisConsumerClient : IConsumerClient
    {
        private readonly string _groupId;
        private readonly ILogger<RedisConsumerClient> _logger;
        private readonly IOptions<RedisMQOptions> _options;
        private readonly IRedisStreamManager _redis;
        private string[] _topics = default!;

        public RedisConsumerClient(string groupId,
            IRedisStreamManager redis,
            IOptions<RedisMQOptions> options,
            ILogger<RedisConsumerClient> logger
        )
        {
            _groupId = groupId;
            _redis = redis;
            _options = options;
            _logger = logger;
        }

        public event EventHandler<TransportMessage>? OnMessageReceived;

        public event EventHandler<LogMessageEventArgs>? OnLog;
        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            foreach (var topic in topics)
                _redis.CreateStreamWithConsumerGroupAsync(topic, _groupId).GetAwaiter().GetResult();

            _topics = topics.ToArray();
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _ = HandleForPendingMessageAsync(timeout,cancellationToken);
            _ = ListeningForMessagesAsync(timeout, cancellationToken);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private async Task HandleForPendingMessageAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                var positions = _topics.Select(it => new StreamPosition(it, StreamPosition.Beginning)).ToArray();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    //first time, we want to read our pending messages, in case we crashed and are recovering.
                    var pendingMsgInfos =
                        (await _redis.PollStreamsPendingMessagesInfoAsync(_topics, _groupId,positions, cancellationToken)).ToArray();
                    var pendingMessages =
                    await _redis.PollStreamsPendingMessagesAsync(_topics, _groupId,positions,pendingMsgInfos, cancellationToken);
                    await ConsumePendingMessages(pendingMessages);
                    for (var i = 0; i < positions.Length; i++)
                    {
                        if(pendingMessages[_topics[i]].HasValue&&pendingMsgInfos[i].MessageId.HasValue)
                            positions[i]=new StreamPosition(_topics[i], pendingMsgInfos[i].MessageId);
                        else
                            positions[i]=new StreamPosition(_topics[i], StreamPosition.Beginning);
                    }
                    if(pendingMessages.All(it=>it.Value.HasValue)==false)
                        cancellationToken.WaitHandle.WaitOne(timeout*3);
                }
            });
           
        }

        private async Task ConsumePendingMessages(Dictionary<string, StreamEntry?> pendingMsgs)
        {
            foreach (var item in pendingMsgs)
            {
                if (item.Value.HasValue)
                {
                    var message = RedisMessage.Create(item.Value!.Value, _groupId);
                    try
                    {
                        OnMessageReceived?.Invoke((item.Key, _groupId, item.Value.Value.Id.ToString()), message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,"Error while consuming message");
                    }
                }
            }
        }

        public void Commit(object sender)
        {
            var (stream, group, id) = ((string stream, string group, string id))sender;

            _redis.Ack(stream, group, id).GetAwaiter().GetResult();
        }

        public void Reject(object? sender)
        {
            // ignore
        }

        public void Dispose()
        {
            //ignore
        }

        private async Task ListeningForMessagesAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            //Once we consumed our history, we can start getting new messages.
            var newMsgs = _redis.PollStreamsLatestMessagesAsync(_topics, _groupId, timeout, cancellationToken);

            _ = ConsumeMessages(newMsgs, StreamPosition.NewMessages);
        }

        private async Task ConsumeMessages(IAsyncEnumerable<IEnumerable<StackExchange.Redis.RedisStream>> streamsSet, RedisValue position)
        {
            await foreach (var set in streamsSet)
            {
                foreach (var stream in set)
                {
                    foreach (var entry in stream.Entries)
                    {
                        if (entry.IsNull)
                        {
                            return;
                        }

                        try
                        {
                            var message = RedisMessage.Create(entry, _groupId);
                            await _redis.TryLockMessageAsync(stream.Key.ToString(), _groupId, entry.Id.ToString(),
                                TimeSpan.FromSeconds(_options.Value.LockMessageSecond));
                            OnMessageReceived?.Invoke((stream.Key.ToString(), _groupId, entry.Id.ToString()), message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message, ex);
                            var logArgs = new LogMessageEventArgs
                            {
                                LogType = MqLogType.ConsumeError,
                                Reason = ex.ToString()
                            };
                            OnLog?.Invoke(entry, logArgs);
                        }
                        finally
                        {
                            var positionName = position == StreamPosition.Beginning
                                ? nameof(StreamPosition.Beginning)
                                : nameof(StreamPosition.NewMessages);
                            _logger.LogDebug($"Redis stream entry [{entry.Id}] [position : {positionName}] was delivered.");
                        }
                    }
                }
            }
        }
    }
}