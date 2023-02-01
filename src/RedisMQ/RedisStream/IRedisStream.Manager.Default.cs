using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisMQ.Messages;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    internal class RedisStreamManager : IRedisStreamManager
    {
        private readonly IRedisConnectionPool _connectionsPool;
        private readonly ILogger<RedisStreamManager> _logger;
        private readonly RedisMQOptions _options;
        private IConnectionMultiplexer? _redis;
        private const string DeadLetterTopicName="redismq:topic:DeadLetter"; 
        public RedisStreamManager(IRedisConnectionPool connectionsPool, IOptions<RedisMQOptions> options,
            ILogger<RedisStreamManager> logger)
        {
            _options = options.Value;
            _connectionsPool = connectionsPool;
            _logger = logger;
        }

        public async Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            var database = _redis!.GetDatabase();
            await database.TryGetOrCreateStreamConsumerGroupAsync(stream, consumerGroup)
                .ConfigureAwait(false);
        }

        public async Task PublishAsync(string stream, NameValueEntry[] message)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            await _redis!.GetDatabase().StreamAddAsync(stream, message,maxLength:_options.MaxQueueLength,useApproximateMaxLength:true)
                .ConfigureAwait(false);
        }

        public async IAsyncEnumerable<IEnumerable<StackExchange.Redis.RedisStream>> PollStreamsLatestMessagesAsync(
            string[] streams,
            string consumerGroup, TimeSpan pollDelay, [EnumeratorCancellation] CancellationToken token)
        {
            var positions = streams.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages));

            while (true)
            {
                var result = await TryReadConsumerGroupAsync(consumerGroup, positions.ToArray(), token)
                    .ConfigureAwait(false);

                yield return result;

                token.WaitHandle.WaitOne(pollDelay);
            }
        }

        public async Task<IEnumerable<StreamPendingMessageInfo>> PollStreamsPendingMessagesInfoAsync(
            string[] streams,
            string consumerGroup,StreamPosition[] positions, [EnumeratorCancellation] CancellationToken token)
        {
            
            var db = _redis!.GetDatabase();
            var result = new List<StreamPendingMessageInfo>(positions.Length);
            foreach (var position in positions)
            {
                var pendingMessages = await db.StreamPendingMessagesAsync(position.Key,
                    consumerGroup,
                    count: 1,
                    consumerName: consumerGroup,
                    minId: position.Position);
                result.Add(pendingMessages.FirstOrDefault());
            }
            return result;
        }

        public async Task<Dictionary<string, StreamEntry?>> PollStreamsPendingMessagesAsync(string[] topics,
            string groupId, StreamPosition[] positions,
            StreamPendingMessageInfo[] streamPendingMessageInfos,
            CancellationToken cancellationToken)
        {
            var db=_redis!.GetDatabase();
            Dictionary<string,StreamEntry?> res = new();
            for (var i = 0; i < topics.Length; i++)
            {
              
                if(await TryLockMessageAsync(topics[i],groupId,streamPendingMessageInfos[i].MessageId.ToString(),TimeSpan.FromSeconds(_options.LockMessageSecond)))
                {
                    var msg = await db.StreamReadGroupAsync(new RedisKey(topics[i]), new RedisValue(groupId),
                        new RedisValue(groupId), positions[i].Position, 1);
                    res.Add(topics[i], msg.Length>0?msg[0]:null);
                }
                else
                {
                    res.Add(topics[i],null);
                }
                

            }
            return res;
        }

        public async Task<List<string>> PollStreamsFailedMessagesIdAsync(string topic, string groupId,
            CancellationToken cancellationToken)
        {
            await ConnectAsync()
                .ConfigureAwait(false);
            var db=_redis.GetDatabase();
            List<string> res = new();
            var msgInfos = await db.StreamPendingMessagesAsync(topic,groupId,1000,groupId,StreamPosition.Beginning);
            var ids= msgInfos.AsQueryable().Where(it => it.DeliveryCount > _options.FailedRetryCount).Select(it=>it.MessageId.ToString()).ToList();
            return ids;
        }

        public async Task<StreamEntry> PollStreamsCertainMessageAsync(string topic, string groupId, string messageId,
            CancellationToken cancellationToken)
        {
            await ConnectAsync()
                .ConfigureAwait(false);
            var db=_redis.GetDatabase();
            var msgId=new MessageId(messageId);
            var msg = await db.StreamReadGroupAsync(topic,groupId,groupId,msgId.GetPreviousMessageId(),1);
            return msg.FirstOrDefault();
        }

        public async Task<bool> TryLockMessageAsync(string topic, string groupName, string messageId, TimeSpan lockTime)
        {
            await ConnectAsync().ConfigureAwait(false);
            return await _redis.GetDatabase().StringSetAsync($"RedisMQ:{topic}:{groupName}:{messageId}", "1",
                lockTime, When.NotExists);
        }

        public async Task<bool> PublishAsync(Message message)
        {
            await ConnectAsync()
                .ConfigureAwait(false);

            //The object returned from GetDatabase is a cheap pass - thru object, and does not need to be stored
            await _redis!.GetDatabase().StreamAddAsync(message.GetName(),   "value",JsonSerializer.Serialize(message)
                ,maxLength:_options.MaxQueueLength,useApproximateMaxLength:true)
                .ConfigureAwait(false);
            return true;
        }

        public async Task TransferFailedMessageToDeadLetterAsync(TransportMessage msg,string messageId)
        {
            await PublishAsync(DeadLetterTopicName, msg.AsStreamEntries());
            await Ack(msg.GetName(), msg.GetGroup(), messageId);
        }

        public async Task Ack(string stream, string consumerGroup, string messageId)
        {
            await ConnectAsync()
                .ConfigureAwait(false);
            await _redis!.GetDatabase().StreamAcknowledgeAsync(stream, consumerGroup, messageId)
                .ConfigureAwait(false);
        }

        private async Task<IEnumerable<StackExchange.Redis.RedisStream>> TryReadConsumerGroupAsync(string consumerGroup,
            StreamPosition[] positions, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var createdPositions = new List<StreamPosition>();

                await ConnectAsync()
                    .ConfigureAwait(false);

                var database = _redis!.GetDatabase();

                await foreach (var position in database
                                   .TryGetOrCreateConsumerGroupPositionsAsync(positions, consumerGroup, _logger)
                                   .ConfigureAwait(false).WithCancellation(token))
                {
                    createdPositions.Add(position);
                }

                if (!createdPositions.Any()) return Array.Empty<StackExchange.Redis.RedisStream>();
                //calculate keys HashSlots to start reading per HashSlot
                var groupedPositions = createdPositions.GroupBy(s => _redis.GetHashSlot(s.Key))
                    .Select(group => database.StreamReadGroupAsync(group.ToArray(), consumerGroup, consumerGroup,
                        (int)_options.StreamEntriesCount));

                var readSet = await Task.WhenAll(groupedPositions)
                    .ConfigureAwait(false);

                return readSet.SelectMany(set => set);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Redis error when trying read consumer group {consumerGroup}");
            }

            return Array.Empty<StackExchange.Redis.RedisStream>();
        }

        private async Task ConnectAsync()
        {
            _redis = await _connectionsPool.ConnectAsync()
                .ConfigureAwait(false);
        }
        
    }
}