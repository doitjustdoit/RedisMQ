using System;
using System.Collections.Generic;
using System.Text.Json;
using LZH.RedisMQ.Messages;
using StackExchange.Redis;

namespace LZH.RedisMQ.RedisStream
{
    internal static class RedisMessage
    {
        private const string Headers = "headers";
        private const string Body = "body";

        public static NameValueEntry[] AsStreamEntries(this Message message)
        {
            return new[]
            {
                new NameValueEntry(Headers, ToJson(message.Headers)),
                new NameValueEntry(Body, ToJson(message.Value))
            };
        }

        public static Message Create(StreamEntry streamEntry, string? groupId = null)
        {
            var headersRaw = streamEntry[Headers];
            if (headersRaw.IsNullOrEmpty)
            {
                throw new ArgumentException($"Redis stream entry with id {streamEntry.Id} missing cap headers");
            }
                
            var headers = JsonSerializer.Deserialize<IDictionary<string, string?>>(headersRaw)!;

            var bodyRaw = streamEntry[Body];

            var body = !bodyRaw.IsNullOrEmpty ? JsonSerializer.Deserialize<byte[]>(bodyRaw) : null;

            headers.TryAdd(Messages.Headers.Group, groupId);

            return new Message(headers, body);
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