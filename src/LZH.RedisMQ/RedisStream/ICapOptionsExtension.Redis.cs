// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using DotNetCore.CAP.RedisStreams;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using LZH.RedisMQ;
using LZH.RedisMQ.RedisStream;
using LZH.RedisMQ.Transport;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    internal class RedisOptionsExtension 
    {
        private readonly Action<RedisMQOptions> _configure;

        public RedisOptionsExtension(Action<RedisMQOptions> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<RedisMessageQueueMakerService>();
            services.AddSingleton<IRedisStreamManager, RedisStreamManager>();
            services.AddSingleton<IConsumerClientFactory, RedisConsumerClientFactory>();
            services.AddSingleton<ITransport, RedisTransport>();
            services.AddSingleton<IRedisConnectionPool, RedisConnectionPool>();
            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IPostConfigureOptions<CapRedisOptions>, CapRedisOptionsPostConfigure>());
            services.AddOptions<CapRedisOptions>().Configure(_configure);
        }
    }

}