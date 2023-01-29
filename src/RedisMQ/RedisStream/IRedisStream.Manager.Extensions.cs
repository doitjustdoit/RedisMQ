// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.RedisStreams;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    internal static class RedisStreamManagerExtensions
    {
        public static async IAsyncEnumerable<StreamPosition> TryGetOrCreateConsumerGroupPositionsAsync(this IDatabase database, StreamPosition[] positions, string consumerGroup, ILogger logger)
        {
            foreach (var position in positions)
            {
                var created = false;
                try
                {
                    var isConnected = database.IsConnected("ping");
                    await database.TryGetOrCreateStreamConsumerGroupAsync(position.Key, consumerGroup)
                        .ConfigureAwait(false);

                    created = true;
                }
                catch (Exception ex)
                {
                    if (ex.GetRedisErrorType() == RedisErrorTypes.Unknown)
                    {
                        logger?.LogError(ex,
                        $"Redis error while creating consumer group [{consumerGroup}] of stream [{position.Key}]");
                    }
                }

                if (created)
                    yield return position;
            }
        }

        public static async Task TryGetOrCreateStreamConsumerGroupAsync(this IDatabase database, RedisKey stream, RedisValue consumerGroup)
        {
            var streamExist = await database.KeyExistsAsync(stream)
                .ConfigureAwait(false);
            if (streamExist)
            {
                await database.TryGetOrCreateStreamGroupAsync(stream, consumerGroup)
                    .ConfigureAwait(false);
                return;
            }
            try
            {
                await database.StreamCreateConsumerGroupAsync(stream, consumerGroup, StreamPosition.NewMessages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.GetRedisErrorType() != RedisErrorTypes.GroupAlreadyExists) throw;
            }
        }

        private static async Task TryGetOrCreateStreamGroupAsync(this IDatabase database, RedisKey stream, RedisValue consumerGroup)
        {
            try
            {
                var groupInfo = await database.StreamGroupInfoAsync(stream);
                if (groupInfo.Any(g => g.Name == consumerGroup))
                    return;
                await database.StreamCreateConsumerGroupAsync(stream, consumerGroup, StreamPosition.NewMessages)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.GetRedisErrorType() is var type && type == RedisErrorTypes.NoGroupInfoExists)
                {
                    await database.TryGetOrCreateStreamConsumerGroupAsync(stream, consumerGroup)
                        .ConfigureAwait(false);
                    return;
                }
                throw;
            }
        }
    }
}