// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisMQ
{
    /// <summary>
    /// A publish service for publish a message to CAP.
    /// </summary>
    public interface IRedisPublisher
    {
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// CAP transaction context object
        /// </summary>  
        AsyncLocal<IRedisTransaction> Transaction { get; }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="headers"></param>
        /// <typeparam name="T"></typeparam>
        Task PublishAsync<T>(string name, T? value, IDictionary<string, string?> headers);

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="callbackName">消费者处理后要回调的topic</param>
        /// <typeparam name="T"></typeparam>
        Task PublishAsync<T>(string name, T? value, string? callbackName = null);
    }
}