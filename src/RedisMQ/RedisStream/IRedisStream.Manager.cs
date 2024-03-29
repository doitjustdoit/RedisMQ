﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedisMQ.Messages;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    public interface IRedisStreamManager
    {
        Task CreateStreamWithConsumerGroupAsync(string stream, string consumerGroup);
        Task PublishAsync(string stream, NameValueEntry[] message);

        IAsyncEnumerable<IEnumerable<StackExchange.Redis.RedisStream>> PollStreamsLatestMessagesAsync(string[] streams, string consumerGroup,
            TimeSpan pollDelay, CancellationToken token);


        Task Ack(string stream, string consumerGroup, string messageId);

        Task<IEnumerable<StreamPendingMessageInfo>> PollStreamsPendingMessagesInfoAsync(
            string[] streams,
            string consumerGroup, StreamPosition[] positions, CancellationToken token);

        Task<Dictionary<string, StreamEntry?>> PollStreamsPendingMessagesAsync(string[] topics, string groupId,
            StreamPosition[] positions, StreamPendingMessageInfo[] streamPendingMessageInfos,
            CancellationToken cancellationToken);

        /// <returns>返回值topic,group name,stream message id</returns>
        Task<List<string>> PollStreamsFailedMessagesIdAsync(string topic, string groupId,
            CancellationToken cancellationToken);
        
        Task<StreamEntry> PollStreamsCertainMessageAsync(string topic, string groupId,string messageId,
            CancellationToken cancellationToken);
        Task<bool> TryLockMessageAsync(string topic, string groupName, string messageId, TimeSpan lockTime);
        Task TransferFailedMessageToDeadLetterAsync(TransportMessage msg, string messageId);
       
    }
}