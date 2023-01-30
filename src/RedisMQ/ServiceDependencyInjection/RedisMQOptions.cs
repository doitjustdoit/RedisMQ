using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using RedisMQ.Messages;
using StackExchange.Redis;

namespace RedisMQ
{
    public class RedisMQOptions
    {

        public RedisMQOptions()
        {
            DefaultGroupName = "redis.queue." + Assembly.GetEntryAssembly()?.GetName().Name.ToLower();
        }
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();
        
        /// <summary>
        ///     Gets or sets the native options of StackExchange.Redis
        /// </summary>
        public ConfigurationOptions? Configuration { get; set; }

        internal string Endpoint { get; set; } = default!;

        /// <summary>
        ///     Gets or sets the count of entries consumed from stream
        /// </summary>
        public uint StreamEntriesCount { get; set; } = 10;

        /// <summary>
        ///     Gets or sets the number of connections that can be used with redis server
        /// </summary>
        public uint ConnectionPoolSize { get; set; } = 10;

        public string? TopicNamePrefix { get; set; }
        public int ConsumerThreadCount { get; set; } = 1;
        
        public Action<FailedInfo>? FailedThresholdCallback { get; set; }
        public uint FailedRetryCount { get; set; } = 10;
        public string? GroupNamePrefix { get; set; }
        public string DefaultGroupName { get; set; }
        public string Version { get; set; } = "v1";
        public bool UseDispatchingPerGroup { get; set; } = false;
        // 是否开启提前拉取模式 开启后根据ConsumerThreadCount*300的数量拉取消息消费
        public bool EnableConsumerPrefetch { get; set; } = true;
        // 多少个线程用于发送消息
        public int ProducerThreadCount { get; set; } = 1;
        public double FailedRetryInterval { get; set; } = 2;

        /// <summary>
        /// Gets the extensions.
        /// </summary>
        /// <value>The extensions.</value>
        public IList<IRedisMQOptionsExtension> Extensions { get; } = new List<IRedisMQOptionsExtension>();
    }
}