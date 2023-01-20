using System;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Processor;
using LZH.RedisMQ.RedisStream;
using LZH.RedisMQ.Serialization;
using LZH.RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LZH.RedisMQ;

public static class ServiceDependencyInjection
{
    public static IServiceCollection AddRedisMQ(this IServiceCollection services, Action<RedisMQOptions> setupAction)
    {
        
        services.AddSingleton(_ => services);

        services.TryAddSingleton<IRedisPublisher, RedisMQPublisher>();

        services.TryAddSingleton<IConsumerServiceSelector, ConsumerServiceSelector>();
        services.TryAddSingleton<ISubscribeInvoker, SubscribeInvoker>();
        services.TryAddSingleton<MethodMatcherCache>();

        services.TryAddSingleton<IConsumerRegister, ConsumerRegister>();

        //Processors
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IProcessingServer, IDispatcher>(sp => sp.GetRequiredService<IDispatcher>()));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IProcessingServer, IConsumerRegister>(sp =>
                sp.GetRequiredService<IConsumerRegister>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessingServer, RedisProcessingServer>());

        //Queue's message processor
        services.TryAddSingleton<MessageNeedToRetryProcessor>();
        services.TryAddSingleton<TransportCheckProcessor>();

        //Sender
        services.TryAddSingleton<IMessageSender, MessageSender>();

        services.TryAddSingleton<ISerializer, JsonUtf8Serializer>();

        // Warning: IPublishMessageSender need to inject at extension project. 
        services.TryAddSingleton<ISubscribeExecutor, SubscribeExecutor>();

        //Options and extension service
        var options = new RedisMQOptions();
        setupAction(options);
        services.Configure(setupAction);

        //Executors
        if (options.UseDispatchingPerGroup)
            services.TryAddSingleton<IDispatcher, DispatcherPerGroup>();
        else
            services.TryAddSingleton<IDispatcher, Dispatcher>();

        services.Configure(setupAction);

        //Startup and Hosted 
        services.AddSingleton<Bootstrapper>();
        services.AddHostedService(sp => sp.GetRequiredService<Bootstrapper>());
        services.AddSingleton<IBootstrapper>(sp => sp.GetRequiredService<Bootstrapper>());
        
        services.AddSingleton<IRedisStreamManager, RedisStreamManager>();
        services.AddSingleton<IConsumerClientFactory, RedisConsumerClientFactory>();
        services.AddSingleton<ITransport, RedisTransport>();
        services.AddSingleton<IRedisConnectionPool, RedisConnectionPool>();
        return services;
    }
}