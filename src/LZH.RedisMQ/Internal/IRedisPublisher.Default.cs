﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LZH.RedisMQ.Internal
{
    internal class RedisMQPublisher : IRedisPublisher
    {
        private readonly IDispatcher _dispatcher;
        private readonly RedisMQOptions _redisMQOptions;
        private readonly ILogger<RedisMQPublisher> _logger;

        public RedisMQPublisher(IServiceProvider service)
        {
            ServiceProvider = service;
            _dispatcher = service.GetRequiredService<IDispatcher>();
            _logger= service.GetRequiredService<ILogger<RedisMQPublisher>>();
            _redisMQOptions = service.GetRequiredService<IOptions<RedisMQOptions>>().Value;
            Transaction = new AsyncLocal<IRedisTransaction>();
        }

        public IServiceProvider ServiceProvider { get; }

        public AsyncLocal<IRedisTransaction> Transaction { get; }

        public Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, headers), cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? value, string? callbackName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Publish(name, value, callbackName), cancellationToken);
        }

        public void Publish<T>(string name, T? value, string? callbackName = null)
        {
            var header = new Dictionary<string, string?>
            {
                {Headers.CallbackName, callbackName}
            };

            Publish(name, value, header);
        }

        public void Publish<T>(string name, T? value, IDictionary<string, string?> headers)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!string.IsNullOrEmpty(_redisMQOptions.TopicNamePrefix))
            {
                name = $"{_redisMQOptions.TopicNamePrefix}.{name}";
            }

            if (!headers.ContainsKey(Headers.MessageId))
            {
                var messageId = SnowflakeId.Default().NextId().ToString();
                headers.Add(Headers.MessageId, messageId);
            }

         
            headers.Add(Headers.Type, typeof(T).Name);
            headers.Add(Headers.SentTime, DateTimeOffset.Now.ToString());

            var message = new Message(headers, value);

            long? tracingTimestamp = null;
            try
            {

                if (Transaction.Value?.DbTransaction == null)
                {

                    _dispatcher.EnqueueToPublish(message);
                }
                else
                {
                    var transaction = (RedisTransactionBase)Transaction.Value;


                    transaction.AddToSent(message);

                    if (transaction.AutoCommit)
                    {
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                TracingError(tracingTimestamp, message, e);

                throw;
            }
        }

        #region tracing

        private void TracingError(long? tracingTimestamp, Message message, Exception ex)
        {
            _logger.LogError(ex,$"timestamp: {tracingTimestamp} message: {message}  exception message: {ex.Message}");
        }

        #endregion
    }
}