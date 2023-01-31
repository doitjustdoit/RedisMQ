// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using RedisMQ.Messages;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    internal static class RedisMessage
    {
        private const string Headers = "headers";
        private const string Body = "body";

        public static NameValueEntry[] AsStreamEntries(this TransportMessage message)
        {
            return new[]
            {
                new NameValueEntry(Headers,  ToJson(message.Headers)),
                // new NameValueEntry(Headers,  ToJson(message.Headers)),
                new NameValueEntry(Body, message.Body)
            };
        }

        public static TransportMessage Create(StreamEntry streamEntry, string? groupId = null)
        {
            var headersRaw = streamEntry[Headers];
            if (headersRaw.IsNullOrEmpty)
            {
                throw new ArgumentException($"Redis stream entry with id {streamEntry.Id} missing cap headers");
            }
                
            var headers = JsonSerializer.Deserialize<IDictionary<string, string?>>(headersRaw!)!;


            var body = streamEntry[Body].ToString();

            headers.TryAdd(Messages.Headers.Group, groupId);

            return new TransportMessage(headers, body);
        }

        private static RedisValue ToJson(object? obj)
        {
            if (obj == null)
            {
                return RedisValue.Null;
            }
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
    }
}