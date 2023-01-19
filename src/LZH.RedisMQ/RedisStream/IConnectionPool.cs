﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using StackExchange.Redis;

namespace LZH.RedisMQ.RedisStream
{
    internal interface IRedisConnectionPool
    {
        Task<IConnectionMultiplexer> ConnectAsync();
    }
}