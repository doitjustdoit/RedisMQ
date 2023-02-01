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

    /// <summary>
    /// 发送消息 成功
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PublishSuccess([FromQuery]string msg="hello world")
    {
        _redisPublisher.PublishAsync("test_success",new TransDto());
        return Ok();
    }
    /// <summary>
    /// 发送消息 失败
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PublishFailed([FromQuery] string msg="hello world")
    {
        _redisPublisher.PublishAsync("test_failed",new TransDto());
        return Ok();
    }
   
    
    public class  TransDto
    {
        public string Name { get; set; } = "lizhenghao";
        public int Age { get; set; } = 27;
    }
    
}