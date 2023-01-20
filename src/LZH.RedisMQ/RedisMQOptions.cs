using System;
using System.Text.Json;
using LZH.RedisMQ.Messages;
using StackExchange.Redis;

namespace LZH.RedisMQ
{
    public class RedisMQOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
        
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
        public bool EnableConsumerPrefetch { get; set; } = true;
        public int ProducerThreadCount { get; set; }
        public double FailedRetryInterval { get; set; } = 2;
    }
}