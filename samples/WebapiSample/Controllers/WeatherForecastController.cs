using LZH.RedisMQ;
using Microsoft.AspNetCore.Mvc;

namespace WebapiSample.Controllers;

[ApiController]
[Route("[controller]")]
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

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpPost]
    public IActionResult Publish([FromQuery]string msg="hello world")
    {
        _redisPublisher.PublishWithQueue("test",new TransDto());
        return Ok();
    }
    [NonAction]
    [RedisSubscribe("test")]
    public void Test(TransDto msg,[FromRedis] RedisHeader headers)
    {
        Console.WriteLine($"received from {msg.Name} - {msg.Age}");
    }
    
    public class  TransDto
    {
        public string Name { get; set; } = "lizhenghao";
        public int Age { get; set; } = 27;
    }
    
}