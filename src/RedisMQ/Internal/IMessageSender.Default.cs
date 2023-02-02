// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RedisMQ.Insternal;
using RedisMQ.Messages;
using RedisMQ.Serialization;
using RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RedisMQ.Internal
{
    internal class MessageSender : IMessageSender
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly ISerializer _serializer;
        private readonly ITransport _transport;
        private readonly IOptions<RedisMQOptions> _options;


        public MessageSender(
            ILogger<MessageSender> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            _options = serviceProvider.GetRequiredService<IOptions<RedisMQOptions>>();
            _serializer = serviceProvider.GetRequiredService<ISerializer>();
            _transport = serviceProvider.GetRequiredService<ITransport>();
        }

        public async Task<OperateResult> SendAsync(Message message)
        {
            var transportMsg = await _serializer.SerializeAsync(message);

            var result = await _transport.SendAsync(transportMsg);

            return result;
        }

        #region tracing

        private void TracingError(long? tracingTimestamp, TransportMessage message, OperateResult result)
        {
            _logger.LogError($"timestamp: {tracingTimestamp} message: {message} op result: {result}");
        }

        #endregion
    }
}