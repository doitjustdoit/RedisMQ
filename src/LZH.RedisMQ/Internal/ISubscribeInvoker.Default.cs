// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace LZH.RedisMQ.Internal
{
    public class SubscribeInvoker : ISubscribeInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<string, ObjectMethodExecutor> _executors;

        public SubscribeInvoker(IServiceProvider serviceProvider, ISerializer serializer)
        {
            _serviceProvider = serviceProvider;
            _serializer = serializer;
            _executors = new ConcurrentDictionary<string, ObjectMethodExecutor>();
        }

        public async Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var methodInfo = context.ConsumerDescriptor.MethodInfo;
            var reflectedTypeHandle = methodInfo.ReflectedType!.TypeHandle.Value;
            var methodHandle = methodInfo.MethodHandle.Value;
            var key = $"{reflectedTypeHandle}_{methodHandle}";

            var executor = _executors.GetOrAdd(key, _ => ObjectMethodExecutor.Create(methodInfo, context.ConsumerDescriptor.ImplTypeInfo));

            using var scope = _serviceProvider.CreateScope();

            var provider = scope.ServiceProvider;

            var obj = GetInstance(provider, context);

            var message = context.DeliverMessage;
            var parameterDescriptors = context.ConsumerDescriptor.Parameters;
            var executeParameters = new object?[parameterDescriptors.Count];
            for (var i = 0; i < parameterDescriptors.Count; i++)
            {
                var parameterDescriptor = parameterDescriptors[i];
                if (parameterDescriptor.IsFromRedisHeader)
                {
                    executeParameters[i] = GetRedisProvidedParameter(parameterDescriptor, message, cancellationToken);
                }
                else
                {
                    if (message.Value != null)
                    {
                        if (_serializer.IsJsonType(message.Value))  // use ISerializer when reading from storage, skip other objects if not Json
                        {
                            executeParameters[i] = _serializer.Deserialize(message.Value, parameterDescriptor.ParameterType);
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(parameterDescriptor.ParameterType);
                            if (converter.CanConvertFrom(message.Value.GetType()))
                            {
                                executeParameters[i] = converter.ConvertFrom(message.Value);
                            }
                            else
                            {
                                if (parameterDescriptor.ParameterType.IsInstanceOfType(message.Value))
                                {
                                    executeParameters[i] = message.Value;
                                }
                                else
                                {
                                    executeParameters[i] = Convert.ChangeType(message.Value, parameterDescriptor.ParameterType);
                                }
                            }
                        }
                    }
                }
            }

            
            object? resultObj = await ExecuteWithParameterAsync(executor, obj, executeParameters);

            return new ConsumerExecutedResult(resultObj, message.GetId(), message.GetCallbackName());
        }

        private static object GetRedisProvidedParameter(ParameterDescriptor parameterDescriptor, Message message,
            CancellationToken cancellationToken)
        {
            if (typeof(CancellationToken).IsAssignableFrom(parameterDescriptor.ParameterType))
            {
                return cancellationToken;
            }

            if (parameterDescriptor.ParameterType.IsAssignableFrom(typeof(RedisHeader)))
            {
                return new RedisHeader(message.Headers);
            }

            throw new ArgumentException(parameterDescriptor.Name);
        }

        protected virtual object GetInstance(IServiceProvider provider, ConsumerContext context)
        {
            var srvType = context.ConsumerDescriptor.ServiceTypeInfo?.AsType();
            var implType = context.ConsumerDescriptor.ImplTypeInfo.AsType();

            object? obj = null;
            if (srvType != null)
            {
                obj = provider.GetServices(srvType).FirstOrDefault(o => o?.GetType() == implType);
            }

            if (obj == null)
            {
                obj = ActivatorUtilities.GetServiceOrCreateInstance(provider, implType);
            }

            return obj;
        }

        private async Task<object?> ExecuteWithParameterAsync(ObjectMethodExecutor executor, object @class, object?[] parameter)
        {
            if (executor.IsMethodAsync)
            {
                return await executor.ExecuteAsync(@class, parameter);
            }

            return executor.Execute(@class, parameter);
        }
    }
}