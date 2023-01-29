using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using RedisMQ.Messages;
using RedisMQ.RedisStream;
using RedisMQ.Serialization;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace RedisMQ.Benchmarks;


// should use command : dotnet test --logger "console;verbosity=detailed" -c Release

public class PublishBenchmark
{
    private readonly ITestOutputHelper output;

    public PublishBenchmark(ITestOutputHelper output)
    {
        this.output = output;
        var converter = new ConsoleConverter(output);
        Console.SetOut(converter);
    }
    
    [Fact]
    public void BenchmarkTest()
    {
        var logger = new AccumulationLogger();
        

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddLogger(logger)
            .WithOptions(ConfigOptions.Default);

        BenchmarkRunner.Run<PublishBenchmarks>(config);

        output.WriteLine(logger.GetLog());
    }

    [MemoryDiagnoser]
    public class PublishBenchmarks
    {
        private ServiceProvider _provider;
        private IRedisPublisher _publisher;
        private IDispatcher _dispacher;
        private ISerializer _serializer;
        private string _rawData;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddRedisMQ(action =>
            {
                action.Configuration = ConfigurationOptions.Parse("localhost:55000,password=redispw");
            });
            services.AddLogging();
            _provider = services.BuildServiceProvider();
            _publisher = _provider.GetRequiredService<IRedisPublisher>();
            _dispacher = _provider.GetRequiredService<IDispatcher>();
            _serializer = _provider.GetRequiredService<ISerializer>();
            _rawData=_serializer.Serialize(new Message(new Dictionary<string, string?>(),new TestTransDto()));
        }

        [Benchmark]
        public void Publish_1000()
        {
            List<Task> tasks = new();
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(_publisher.PublishAsync("test2", new TestTransDto()));
            }

            Task.WaitAll(tasks.ToArray());
        }
        [Benchmark]
        public void Publish_10000()
        {
            List<Task> tasks = new();
            for (int i = 0; i < 10000; i++)
            {
                tasks.Add(_publisher.PublishAsync("test2", new TestTransDto()));
            }

            Task.WaitAll(tasks.ToArray());
        }
        [Benchmark]
        public void Publish_WithoutSerialization_1000()
        {
            IRedisStreamManager redisStreamManager = _provider.GetRequiredService<IRedisStreamManager>();
            NameValueEntry[] valueEntry = new NameValueEntry[1] { new NameValueEntry("body", _rawData) };

            List<Task> tasks = new();
            for (int i = 0; i < 1000; i++)
            {
                redisStreamManager.PublishAsync("test2", valueEntry);
            }

            Task.WaitAll(tasks.ToArray());
        }
        [Benchmark]
        public void Publish_WithoutSerialization_10000()
        {
            IRedisStreamManager redisStreamManager = _provider.GetRequiredService<IRedisStreamManager>();
            NameValueEntry[] valueEntry = new NameValueEntry[1] { new NameValueEntry("body", _rawData) };

            List<Task> tasks = new();
            for (int i = 0; i < 10000; i++)
            {
                redisStreamManager.PublishAsync("test2", valueEntry);
            }

            Task.WaitAll(tasks.ToArray());
        }
        [Benchmark]
        public void Publish_WithQueue_1000()
        {
            for (int i = 0; i < 1000; i++)
            {
                _publisher.PublishAsync("test2", new TestTransDto());
            }

            while (_dispacher.GetUnPublishedMessagesCount() > 0)
                ;
        }
        [Benchmark]
        public void Publish_WithQueue_10000()
        {
            for (int i = 0; i < 10000; i++)
            {
                _publisher.PublishAsync("test2", new TestTransDto());
            }

            while (_dispacher.GetUnPublishedMessagesCount() > 0)
                ;
        }
    }
}