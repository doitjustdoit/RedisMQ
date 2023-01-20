using System;
using System.Threading.Tasks;
using LZH.RedisMQ.Internal;
using LZH.RedisMQ.Messages;
using LZH.RedisMQ.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LZH.RedisMQ.RedisStream
{
    internal class RedisTransport : ITransport
    {
        private readonly ILogger<RedisTransport> _logger;
        private readonly RedisMQOptions _options;
        private readonly IRedisStreamManager _redis;

        public RedisTransport(IRedisStreamManager redis, IOptions<RedisMQOptions> options,
            ILogger<RedisTransport> logger)
        {
            _redis = redis;
            _options = options.Value;
            _logger = logger;
        }

        public BrokerAddress BrokerAddress => new ("redis", _options.Endpoint);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            try
            {
                await _redis.PublishAsync(message.GetName(), message.AsStreamEntries())
                    .ConfigureAwait(false);

                _logger.LogDebug($"Redis message [{message.GetName()}] has been published.");

                return OperateResult.Success;
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wrapperEx);
            }
        }
    }
}