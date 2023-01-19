using System;
using Microsoft.Extensions.DependencyInjection;

namespace LZH.RedisMQ;

public static class ServiceDependencyInjection
{
    public static IServiceCollection AddRedisMQ(this IServiceCollection serviceCollection, Action<RedisMQOptions> options)
    {
        var redisMQOptions = new RedisMQOptions();
        options(redisMQOptions);
        serviceCollection.AddSingleton(redisMQOptions);
        return serviceCollection;
    }
}