using Microsoft.Extensions.DependencyInjection;

namespace RedisMQ;

public interface IRedisMQOptionsExtension
{
    /// <summary>
    /// Adds the services.
    /// </summary>
    /// <param name="services">Services.</param>
    void AddServices(IServiceCollection services);
}