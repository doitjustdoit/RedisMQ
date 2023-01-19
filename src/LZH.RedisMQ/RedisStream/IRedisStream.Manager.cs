using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LZH.RedisMQ.RedisStream
{
    internal interface IRedisStreamManager
    {
        Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup);
        Task PublishAsync(string stream, NameValueEntry[] message);

        IAsyncEnumerable<IEnumerable<StackExchange.Redis.RedisStream>> PollStreamsLatestMessagesAsync(string[] streams, string consumerGroup,
            TimeSpan pollDelay, CancellationToken token);

        IAsyncEnumerable<IEnumerable<StackExchange.Redis.RedisStream>> PollStreamsPendingMessagesAsync(string[] streams, string consumerGroup,
            TimeSpan pollDelay, CancellationToken token);

        Task Ack(string stream, string consumerGroup, string messageId);
        
    }
}