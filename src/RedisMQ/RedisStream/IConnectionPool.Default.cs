// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace RedisMQ.RedisStream
{
    internal class RedisConnectionPool : IRedisConnectionPool, IDisposable
    {
        private readonly Queue<RedisConnection> _connections = new();

        private readonly ILoggerFactory _loggerFactory;
        private readonly SemaphoreSlim _poolLock = new(1);
        private readonly RedisMQOptions _redisOptions;
        private bool _isDisposed;
        private bool _poolAlreadyConfigured;

        public RedisConnectionPool(IOptions<RedisMQOptions> options, ILoggerFactory loggerFactory)
        {
            _redisOptions = options.Value;
            _loggerFactory = loggerFactory;
            Init().GetAwaiter().GetResult();
        }

        private RedisConnection? QuietConnection
              =>   _poolAlreadyConfigured ? _connections.MinBy( c =>c.ConnectionCapacity) : null;
            
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IConnectionMultiplexer> ConnectAsync()
        {
            if (QuietConnection == null)
            {
                _poolAlreadyConfigured = _connections.Count == _redisOptions.ConnectionPoolSize;
                if (QuietConnection != null)
                    return QuietConnection.Connection;
            }
            return _connections.MinBy(it=>it.ConnectionCapacity)!.Connection;
        }

        public void RefreshConnectionCapacity()
        {
            foreach (var redisConnection in _connections)
            {
                redisConnection.RefreshConnectionCapacity();
            }
        }

        private async Task Init()
        {
            try
            {
                await _poolLock.WaitAsync();

                if (_connections.Any())
                    return;

                for (var i = 0; i < _redisOptions.ConnectionPoolSize; i++)
                {
                    var connection =await RedisConnection.ConnectAsync(_redisOptions,
                        _loggerFactory.CreateLogger<RedisConnection>());

                    _connections.Enqueue(connection);
                }
            }
            finally
            {
                _poolLock.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
                foreach (var connection in _connections)
                {
                    connection.Dispose();
                }

            _isDisposed = true;
        }
    }
}