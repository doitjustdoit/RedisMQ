using RedisMQ;
using WebapiSample.Controllers;

namespace WebapiSample;

public class CustomSubscribeClass:IRedisSubscribe
{
    private readonly ILogger<CustomSubscribeClass> _logger;

    public CustomSubscribeClass(ILogger<CustomSubscribeClass> logger)
    {
        _logger = logger;
    }
    [RedisSubscribe("test_success")]
    public void TestSuccess(WeatherForecastController.TransDto msg,[FromRedis] RedisHeader headers)
    {
        _logger.LogInformation($"received from {msg.Name} - {msg.Age}-{DateTime.Now}");
    }
    [RedisSubscribe("test_failed")]
    public void TestFailed(WeatherForecastController.TransDto msg,[FromRedis] RedisHeader headers)
    {
        _logger.LogInformation($"received from {msg.Name} - {msg.Age}-{DateTime.Now}");
        throw new Exception("test");
    }
}