using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RedisMQ.Serialization.MessagePack;

public static class ServiceDepedencyInjection
{
    public static IServiceCollection WithMessagePack(this IServiceCollection serviceCollection)
    {
        var descriptor =
            new ServiceDescriptor(
                typeof(ISerializer),
                typeof(DefaultMessagePackSerializer),
                ServiceLifetime.Singleton);
        serviceCollection.Replace(descriptor);
        // var descriptorToRemove = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(ISerializer));
        // if(descriptorToRemove is not null)
            // serviceCollection.Remove(descriptorToRemove);
        // serviceCollection.AddSingleton(descriptor);
        return serviceCollection;
    } 
}