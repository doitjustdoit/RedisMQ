// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LZH.RedisMQ.Insternal;
using LZH.RedisMQ.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LZH.RedisMQ.Internal
{
    internal class SubscribeDispatcher : ISubscribeDispatcher
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;
        private readonly RedisMQOptions _options;

        public SubscribeDispatcher(
            ILogger<SubscribeDispatcher> logger,
            IOptions<RedisMQOptions> options,
            IServiceProvider provider)
        {
            _provider = provider;
            _logger = logger;
            _options = options.Value;

            Invoker = _provider.GetRequiredService<ISubscribeInvoker>();
        }

        private ISubscribeInvoker Invoker { get; }

        public Task<OperateResult> DispatchAsync(Message message, CancellationToken cancellationToken)
        {
            var selector = _provider.GetRequiredService<MethodMatcherCache>();
            if (!selector.TryGetTopicExecutor(message.GetName(), message.GetGroup()!, out var executor))
            {
                var error = $"Message (Name:{message.GetName()},Group:{message.GetGroup()}) can not be found subscriber." +
                            $"{Environment.NewLine}";
                _logger.LogError(error);

                TracingError(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), message, null, new Exception(error));

                return Task.FromResult(OperateResult.Failed(new SubscriberNotFoundException(error)));
            }

            return DispatchAsync(message, executor, cancellationToken);
        }

        public async Task<OperateResult> DispatchAsync(Message message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
        {
            bool retry;
            OperateResult result;

            do
            {
                var (shouldRetry, operateResult) = await ExecuteWithoutRetryAsync(message, descriptor, cancellationToken);
                result = operateResult;
                if (result == OperateResult.Success)
                {
                    return result;
                }
                retry = shouldRetry;
            } while (retry);

            return result;
        }

        private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(Message message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.ConsumerExecuting(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name, descriptor.Attribute.Group);

                var sp = Stopwatch.StartNew();

                await InvokeConsumerMethodAsync(message, descriptor, cancellationToken);

                sp.Stop();

                // _logger.ConsumerExecuted(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name, descriptor.Attribute.Group, sp.Elapsed.TotalMilliseconds, message.GetExecutionInstanceId());

                return (false, OperateResult.Success);
            }
            catch (Exception ex)
            {
                _logger.ConsumerExecuteFailed(message.GetName(),message.GetId(),null, ex);

                return (await SetFailedState(message, ex), OperateResult.Failed(ex));
            }
        }

        private async Task<bool> SetFailedState(Message message, Exception ex)
        {
            if (ex is SubscriberNotFoundException)
            {
                message.AddRetry(_options.FailedRetryCount); // not retry if SubscriberNotFoundException
            }

            var needRetry = UpdateMessageForRetry(message);

            message.AddOrUpdateException(ex);
            return needRetry;
        }

        private bool UpdateMessageForRetry(Message message)
        {
            var retries = message.AddRetry();
           
            var retryCount = Math.Min(_options.FailedRetryCount, 3);

            if (retries >= retryCount)
            {
                if (retries == _options.FailedRetryCount)
                {
                    try
                    {
                        _options.FailedThresholdCallback?.Invoke(new FailedInfo
                        {
                            ServiceProvider = _provider,
                            MessageType = MessageType.Subscribe,
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

        private async Task InvokeConsumerMethodAsync(Message message, ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
        {
            var consumerContext = new ConsumerContext(descriptor, message);
            try
            {
                var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken);

                // TODO 暂时不实现RPC
                // if (!string.IsNullOrEmpty(ret.CallbackName))
                // {
                //     var header = new Dictionary<string, string?>()
                //     {
                //         [Headers.CorrelationId] = message.GetId(),
                //         [Headers.CorrelationSequence] = (message.GetCorrelationSequence() + 1).ToString()
                //     };
                //
                //     await _provider.GetRequiredService<IRedisPublisher>().PublishAsync(ret.CallbackName, ret.Result, header, cancellationToken);
                // }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                var e = new SubscriberExecutionFailedException(ex.Message, ex);
                throw e;
            }
        }

        private string? GenerateHostnameInstanceId()
        {
            try
            {
                var hostName = Dns.GetHostName();
                if (hostName.Length <= 50)
                {
                    return hostName;
                }
                return hostName.Substring(0, 50);

            }
            catch (Exception e)
            {
                _logger.LogWarning("Couldn't get host name!", e);
                return null;
            }
        }

        #region tracing

   
        private void TracingError(long? tracingTimestamp, Message message, MethodInfo? method, Exception ex)
        {
            _logger.LogError(ex,$"timestamp: {tracingTimestamp} message: {message}  method: {method}  exception message: {ex.Message}");
        }

        #endregion
    }
}