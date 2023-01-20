// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LZH.RedisMQ.Insternal;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Serialization;
using LZH.RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LZH.RedisMQ.Internal
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
            bool retry;
            OperateResult result;
            do
            {
                var executedResult = await SendWithoutRetryAsync(message);
                result = executedResult.Item2;
                if (result == OperateResult.Success)
                {
                    return result;
                }
                retry = executedResult.Item1;
            } while (retry);

            return result;
        }

        private async Task<(bool, OperateResult)> SendWithoutRetryAsync(Message message)
        {
            var transportMsg = await _serializer.SerializeAsync(message);


            var result = await _transport.SendAsync(transportMsg);

            if (result.Succeeded)
            {
                return (false, OperateResult.Success);
            }
            else
            {
                var needRetry = await SetFailedState(message, result.Exception!);

                return (needRetry, OperateResult.Failed(result.Exception!));
            }
        }

        private async Task<bool> SetFailedState(Message message, Exception ex)
        {
            var needRetry = UpdateMessageForRetry(message);

            message.AddOrUpdateException(ex);

            return needRetry;
        }

        private bool UpdateMessageForRetry(Message message)
        {
            var retries = message.AddRetry();
            // 发送超过3次失败直接放弃
            var retryCount = 3;
            if (retries >= retryCount)
            {
                if (retries == _options.Value.FailedRetryCount)
                {
                    try
                    {
                        _options.Value.FailedThresholdCallback?.Invoke(new FailedInfo
                        {
                            ServiceProvider = _serviceProvider,
                            MessageType = MessageType.Publish,
                            Message = message
                        });

                    }
                    catch (Exception ex)
                    {
                        _logger.ExecutedThresholdCallbackFailed(ex);
                    }
                }
                return false;
            }

            return true;
        }

        #region tracing

        private void TracingError(long? tracingTimestamp, TransportMessage message, BrokerAddress broker, OperateResult result)
        {
            _logger.LogError($"timestamp: {tracingTimestamp} message: {message} broker: {broker} op result: {result}");
        }

        #endregion
    }
}