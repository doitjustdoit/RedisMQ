// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LZH.RedisMQ
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
        /// Asynchronous publish an object message.
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="callbackName">callback subscriber name</param>
        /// <param name="cancellationToken"></param>
        Task PublishAsyncWithQueue<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronous publish an object message with custom headers
        /// </summary>
        /// <typeparam name="T">content object</typeparam>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="headers">message additional headers.</param>
        /// <param name="cancellationToken"></param>
        Task PublishAsyncWithQueue<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default);

        /// <summary>
        /// 通过队列来发送消息，性能较高，但吞吐量大的情况下程序突然宕机会丢失消息
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="callbackName">callback subscriber name</param>
        void PublishWithQueue<T>(string name, T? contentObj, string? callbackName = null);

        /// <summary>
        /// 通过队列来发送消息，性能较高，但吞吐量大的情况下程序突然宕机会丢失消息
        /// </summary>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="value">message body content, that will be serialized. (can be null)</param>
        /// <param name="headers">message additional headers.</param>
        void PublishWithQueue<T>(string name, T? value, IDictionary<string, string?> headers);
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