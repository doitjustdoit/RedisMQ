// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace RedisMQ.Messages
{
    public class Message 
    {
        public Message()
        {
            Headers = new Dictionary<string, string?>();
        }
        public Message(IDictionary<string, string?> headers, object? value)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Value = value;
        }
        public IDictionary<string,string?> Headers { get; set; }
        public object? Value { get; set; }

       
    }
    
    public static class MessageExtensions
    {
        public static string GetId(this Message message)
        {
            return message.Headers[Headers.MessageId]!;
        }
        public static string GetName(this Message message)
        {
            return message.Headers[Headers.MessageName]!;
        }

        public static string? GetStreamMessageId(this Message message)
        {
            return message.Headers[Headers.StreamMessageId];
        }
        public static string? GetGroup(this Message message)
        {
            message.Headers.TryGetValue(Headers.Group, out var value);
            return value;
        }
        public static string? GetCallbackName(this Message message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }
        public static bool HasException(this Message message)
        {
            return message.Headers.ContainsKey(Headers.Exception);
        }

        public static void AddOrUpdateException(this Message message, Exception ex)
        {
            var msg = $"{ex.GetType().Name}-->{ex.Message}";

            message.Headers[Headers.Exception] = msg;
        }
        public static uint GetRetry(this Message message)
        {
            return Convert.ToUInt32( message.Headers[Headers.Retries]);
        }
        public static uint AddRetry(this Message message,uint add=1)
        {
            var exist=message.Headers.TryGetValue(Headers.Retries,out string? retryStr);
            if (exist)
            {
                var retry = Convert.ToUInt32(retryStr);
                retry+=add;
                message.Headers[Headers.Retries] = retry.ToString();
                return retry;
            }

            message.Headers[Headers.Retries] = "1";
            return 1;
        }
        public static void RemoveException(this Message message)
        {
            message.Headers.Remove(Headers.Exception);
        }
        
    }
}