using System.Diagnostics;
using RedisMQ;
using RedisMQ.RedisStream;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace RedisMQ.Benchmarks;

public class SimpleTest
{
    private readonly ITestOutputHelper output;

    public SimpleTest(ITestOutputHelper output)
    {
        this.output = output;
        var services = new ServiceCollection();
        services.AddRedisMQ(action =>
        {
            action.Configuration = ConfigurationOptions.Parse("localhost:55000,password=redispw");
        });
        services.AddLogging();
        _provider = services.BuildServiceProvider();
        _publisher = _provider.GetRequiredService<IRedisPublisher>();
    }

    private ServiceProvider _provider;
    private IRedisPublisher _publisher;

    [Fact]
    public async Task PublishTest()
    {
        for (int i = 0; i < 100; i++)
        {
            await _publisher.PublishAsync("test", new TransDto());
        }
    }
    internal class  TransDto
    {
        public string Name { get; set; } = "lizhenghao";
        public int Age { get; set; } = 27;
    }
    [Fact]
    public void RawPublish()
    {
        Stopwatch sw = new();
        sw.Restart();
        try
        {
            IRedisStreamManager redisStreamManager = _provider.GetRequiredService<IRedisStreamManager>();
            NameValueEntry[] valueEntry = new NameValueEntry[1] { new NameValueEntry("body", "h") };

            List<Task> tasks = new();
            for (int i = 0; i < 50; i++)
            {
                redisStreamManager.PublishAsync("test2", valueEntry);
            }

            Task.WaitAll(tasks.ToArray());
        }
        catch (Exception e)
        {
            Console.WriteLine("res");
            throw;
        }
        output.WriteLine(sw.ElapsedMilliseconds.ToString());
    }

}