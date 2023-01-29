﻿using System.Reflection;
using RedisMQ;
using RedisMQ.Internal;
using RedisMQ.Messages;
using RedisMQ.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace RedisMQ.Test
{
    public class SubscribeInvokerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscribeInvokerTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ISerializer, JsonUtf8Serializer>();
            serviceCollection.AddSingleton<ISubscribeInvoker, SubscribeInvoker>();
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private ISubscribeInvoker SubscribeInvoker => _serviceProvider.GetService<ISubscribeInvoker>();

        [Fact]
        public async Task InvokeTest()
        {
            var descriptor = new ConsumerExecutorDescriptor()
            {
                Attribute = new RedisSubscribeAttribute("fake.output.integer"),
                ServiceTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                ImplTypeInfo = typeof(FakeSubscriber).GetTypeInfo(),
                MethodInfo = typeof(FakeSubscriber).GetMethod(nameof(FakeSubscriber.OutputIntegerSub)),
                Parameters = new List<ParameterDescriptor>()
            };

            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = SnowflakeId.Default().NextId().ToString(),
                [Headers.MessageName] = "fake.output.integer"
            };
            var message = new Message(header, null);
            var context = new ConsumerContext(descriptor, message);

            var ret = await SubscribeInvoker.InvokeAsync(context);
            Assert.Equal(int.MaxValue, ret.Result);
        }
    }

    public class FakeSubscriber : IRedisSubscribe
    {
        [RedisSubscribe("fake.output.integer")]
        public int OutputIntegerSub()
        {
            return int.MaxValue;
        }
    }
}
