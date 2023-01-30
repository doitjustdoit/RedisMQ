using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RedisMQ.Serialization.MessagePack;

public class MessagePackOptionsExtension:IRedisMQOptionsExtension
{
    public void AddServices(IServiceCollection services)
    {
        var descriptor =
            new ServiceDescriptor(
                typeof(ISerializer),
                typeof(DefaultMessagePackSerializer),
                ServiceLifetime.Singleton);
        services.Replace(descriptor);
    }
}