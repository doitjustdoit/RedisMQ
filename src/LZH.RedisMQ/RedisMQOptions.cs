using System.Text.Json;
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
    }
}