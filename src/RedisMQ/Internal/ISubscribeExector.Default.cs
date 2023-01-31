// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RedisMQ.Insternal;
using RedisMQ.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RedisMQ.Internal;

internal class SubscribeExecutor : ISubscribeExecutor
{
  

    private readonly string? _hostName;
    private readonly ILogger _logger;
    private readonly RedisMQOptions _options;
    private readonly IServiceProvider _provider;

    public SubscribeExecutor(
        ILogger<SubscribeExecutor> logger,
        IOptions<RedisMQOptions> options,
        IServiceProvider provider)
    {
        _provider = provider;
        _logger = logger;
        _options = options.Value;

        Invoker = _provider.GetRequiredService<ISubscribeInvoker>();
        _hostName = GenerateHostnameInstanceId();
    }

    private ISubscribeInvoker Invoker { get; }

    public async Task<OperateResult> ExecuteAsync(Message message, ConsumerExecutorDescriptor? descriptor = null, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
        {
            var selector = _provider.GetRequiredService<MethodMatcherCache>();
            if (!selector.TryGetTopicExecutor(message.GetName(), message.GetGroup()!, out descriptor))
            {
                var error =
                    $"Message (Name:{message.GetName()},Group:{message.GetGroup()}) can not be found subscriber." +
                    $"{Environment.NewLine}";
                _logger.LogError(error);

                TracingError(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), message, null, new Exception(error));

                return OperateResult.Failed(new SubscriberNotFoundException(error));
            }
        }

        bool retry;
        OperateResult result;
        

        do
        {
            var (shouldRetry, operateResult) = await ExecuteWithoutRetryAsync(message, descriptor, cancellationToken).ConfigureAwait(false);
            result = operateResult;
            if (result.Equals(OperateResult.Success)) return result;
            retry = shouldRetry;
        } while (retry);

        return result;
    }

    private async Task<(bool, OperateResult)> ExecuteWithoutRetryAsync(Message message,
        ConsumerExecutorDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.ConsumerExecuting(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name,
                descriptor.Attribute.Group);

            var sp = Stopwatch.StartNew();

            await InvokeConsumerMethodAsync(message, descriptor, cancellationToken).ConfigureAwait(false);

            sp.Stop();

            _logger.ConsumerExecuted(descriptor.ImplTypeInfo.Name, descriptor.MethodInfo.Name,
                descriptor.Attribute.Group, sp.Elapsed.TotalMilliseconds, null);

            return (false, OperateResult.Success);
        }
        catch (Exception ex)
        {
            // return (await SetFailedState(message, ex).ConfigureAwait(false), OperateResult.Failed(ex));
            return (false, OperateResult.Failed(ex));
        }
    }

   
    private async Task InvokeConsumerMethodAsync(Message message, ConsumerExecutorDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var consumerContext = new ConsumerContext(descriptor, message);
        try
        {
            var ret = await Invoker.InvokeAsync(consumerContext, cancellationToken).ConfigureAwait(false);
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
            if (hostName.Length <= 50) return hostName;
            return hostName.Substring(0, 50);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Couldn't get host name!");
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