using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisMQ.Internal;
using RedisMQ.Messages;
using StackExchange.Redis;

namespace RedisMQ.Test;


public class PubSubTest
{
    private readonly ServiceProvider _provider;
    private readonly IOptions<RedisMQOptions> _options;

    public PubSubTest()
    {
       
        ServiceCollection serviceCollection = new();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<TestSubscribeClass>();
        serviceCollection.AddRedisMQ(option =>
        {
            option.Configuration = ConfigurationOptions.Parse(Config.ConnectionString);
            option.FailedRetryCount = 2;
            option.FailedThresholdCallback += FailedNotifyCallBack;
            option.FailedRetryInterval = 5;
            option.LockMessageSecond = 5;
        });
        _provider= serviceCollection.BuildServiceProvider();
        _options = _provider.GetRequiredService<IOptions<RedisMQOptions>>();
        var redis=ConnectionMultiplexer.Connect(Config.ConnectionString);
        redis.GetDatabase().Execute("DEL", $"{_options.Value.TopicNamePrefix}:{Config.SuccessTopic}");
        redis.GetDatabase().Execute("DEL", $"{_options.Value.TopicNamePrefix}:{Config.FailedTopic}");
        _provider.GetRequiredService<IBootstrapper>().BootstrapAsync().GetAwaiter().GetResult();
       
    }

    private bool FailedCallBack = false;

    private void FailedNotifyCallBack(TransportMessage msg)
    {
        FailedCallBack = true;
    }

    [Fact]
    public async Task PublishSuccessMessage()
    {
        TestTransDto msg = new();
        var publisher = _provider.GetRequiredService<IRedisPublisher>();
        await publisher.PublishAsync(Config.SuccessTopic, msg);
        AssertExtension.Until(() => TestSubscribeClass.ReceivedSuccessMsgCount == 1, 10);
    }
    [Fact]
    public async Task PublishFailedMessage()
    {
        CancellationTokenSource source = new(300*1000);
        TestTransDto msg = new();
        var publisher = _provider.GetRequiredService<IRedisPublisher>();
        await publisher.PublishAsync(Config.FailedTopic, msg);
        while (TestSubscribeClass.ReceivedFailedMsgCount <= _options.Value.FailedRetryCount)
        {
            source.Token.ThrowIfCancellationRequested();
            await Task.Delay(1000);
        }
        // AssertExtension.Until(() => TestSubscribeClass.ReceivedFailedMsgCount >= _options.Value.FailedRetryCount,30 );
        // AssertExtension.Until(()=>FailedCallBack,30);
    }

    [Fact]
    public async Task ConsumeRepeatTest()
    {
        TestTransDto msg = new();
        var publisher = _provider.GetRequiredService<IRedisPublisher>();
        for (int i = 0; i < 100; i++)
        {
            await publisher.PublishAsync(Config.SuccessRepeatTopic, msg);
        }
        AssertExtension.Until(() => TestSubscribeClass.ReceivedSuccessMsgRepeatCount == 100, 30);

    }
}

internal class TestSubscribeClass : IRedisSubscribe
{
    public static int ReceivedSuccessMsgCount=0;
    public static int ReceivedFailedMsgCount=0;
    public static int ReceivedSuccessMsgRepeatCount = 0;

    [RedisSubscribe(Config.SuccessTopic)]
    public void SubscribeSuccessMessage(TestTransDto msg)
    {
        ReceivedSuccessMsgCount += 1;
    }
    [RedisSubscribe(Config.SuccessRepeatTopic)]
    public void SubscribeSuccessRepeatMessage(TestTransDto msg)
    {
        ReceivedSuccessMsgRepeatCount += 1;
    }


    [RedisSubscribe(Config.FailedTopic)]
    public void SubscribeFailedMessage(TestTransDto msg)
    {
        Console.WriteLine("Call SubscribeFailedMessage");
        ReceivedFailedMsgCount += 1;
        throw new Exception("test exception");
    }
}

internal class TestTransDto
{
    public string Name { get; set; } = "User";
    public int Age { get; set; } = 99;
}