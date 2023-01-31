using RedisMQ;
using Microsoft.AspNetCore.Mvc;

namespace WebapiSample.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class WeatherForecastController : ControllerBase,IRedisSubscribe
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IRedisPublisher _redisPublisher;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,IRedisPublisher redisPublisher)
    {
        _redisPublisher = redisPublisher;
        _logger = logger;
    }


    [HttpPost]
    public IActionResult Publish([FromQuery]string msg="hello world")
    {
        _redisPublisher.PublishAsync("test",new TransDto());
        return Ok();
    }
    [NonAction]
    [RedisSubscribe("test")]
    public void Test(TransDto msg,[FromRedis] RedisHeader headers)
    {
        _logger.LogInformation($"received from {msg.Name} - {msg.Age}");
        throw new Exception("test");
    }
    
    public class  TransDto
    {
        public string Name { get; set; } = "lizhenghao";
        public int Age { get; set; } = 27;
    }
    
}