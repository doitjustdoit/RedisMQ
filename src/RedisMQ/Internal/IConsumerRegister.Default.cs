// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisMQ.Insternal;
using RedisMQ.Messages;
using RedisMQ.Serialization;
using RedisMQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RedisMQ.Internal
{
    internal class ConsumerRegister : IConsumerRegister
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _pollingDelay = TimeSpan.FromSeconds(1);
        private readonly RedisMQOptions _options;

        private IConsumerClientFactory _consumerClientFactory = default!;
        private IDispatcher _dispatcher = default!;
        private ISerializer _serializer = default!;

        private MethodMatcherCache _selector = default!;
        private CancellationTokenSource _cts = new();
        private Task? _compositeTask;
        private bool _disposed;
        private bool _isHealthy = true;

        public ConsumerRegister(ILogger<ConsumerRegister> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = serviceProvider.GetRequiredService<IOptions<RedisMQOptions>>().Value;
        }

        public bool IsHealthy()
        {
            return _isHealthy;
        }

        public void Start(CancellationToken stoppingToken)
        {
            _selector = _serviceProvider.GetRequiredService<MethodMatcherCache>();
            _dispatcher = _serviceProvider.GetRequiredService<IDispatcher>();
            _serializer = _serviceProvider.GetRequiredService<ISerializer>();
            _consumerClientFactory = _serviceProvider.GetRequiredService<IConsumerClientFactory>();

            stoppingToken.Register(Dispose);

            Execute();
        }

        public void Execute()
        {
            var groupingMatches = _selector.GetCandidatesMethodsOfGroupNameGrouped();

            foreach (var matchGroup in groupingMatches)
            {
                ICollection<string> topics;
                try
                {
                    using (var client = _consumerClientFactory.Create(matchGroup.Key))
                    {
                        topics = client.FetchTopics(matchGroup.Value.Select(x => x.TopicName));
                    }
                }
                catch (BrokerConnectionException e)
                {
                    _isHealthy = false;
                    _logger.LogError(e, e.Message);
                    return;
                }

                for (int i = 0; i < _options.ConsumerThreadCount; i++)
                {
                    var topicIds = topics.Select(t => t);
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            using (var client = _consumerClientFactory.Create(matchGroup.Key))
                            {
                                RegisterMessageProcessor(client);

                                client.Subscribe(topicIds);

                                client.Listening(_pollingDelay, _cts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            //ignore
                        }
                        catch (BrokerConnectionException e)
                        {
                            _isHealthy = false;
                            _logger.LogError(e, e.Message);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message);
                        }
                    }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
            }
            _compositeTask = Task.CompletedTask;
        }

        public void ReStart(bool force = false)
        {
            if (!IsHealthy() || force)
            {
                Pulse();

                _cts = new CancellationTokenSource();
                _isHealthy = true;

                Execute();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                Pulse();

                _compositeTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerExceptions[0];
                if (!(innerEx is OperationCanceledException))
                {
                    _logger.ExpectedOperationCanceledException(innerEx);
                }
            }
        }

        public void Pulse()
        {
            _cts.Cancel();  
            _cts.Dispose();
        }

        private void RegisterMessageProcessor(IConsumerClient client)
        {
            // Cannot set subscription to asynchronous
            client.OnMessageReceived += (sender, transportMessage) =>
            {
                _logger.MessageReceived(transportMessage.GetId(), transportMessage.GetName());

                long? tracingTimestamp = null;
                try
                {

                    var name = transportMessage.GetName();
                    var group = transportMessage.GetGroup()!;

                    Message message;

                    var canFindSubscriber = _selector.TryGetTopicExecutor(name, group, out var executor);
                    try
                    {
                        if (!canFindSubscriber)
                        {
                            var error = $"Message can not be found subscriber. Name:{name}, Group:{group}. {Environment.NewLine} see: https://github.com/dotnetcore/CAP/issues/63";
                            var ex = new SubscriberNotFoundException(error);

                            TracingError(tracingTimestamp, transportMessage, ex);

                            throw ex;
                        }

                        var type = executor!.Parameters.FirstOrDefault(x => x.IsFromRedisHeader == false)?.ParameterType;
                        message = _serializer.DeserializeAsync(transportMessage, type).GetAwaiter().GetResult();
                        message.RemoveException();
                    }
                    catch (Exception e)
                    {
                        transportMessage.Headers[Headers.Exception] = e.GetType().Name + "-->" + e.Message;
                        string? dataUri;
                        if (transportMessage.Headers.TryGetValue(Headers.Type, out var val))
                        {
                            if (transportMessage.Body != null)
                            {
                                dataUri = $"data:{val};base64," + transportMessage.Body;
                            }
                            else
                            {
                                dataUri = null;
                            }
                            message = new Message(transportMessage.Headers, dataUri);
                        }
                        else
                        {
                            if (transportMessage.Body != null)
                            {
                                dataUri = "data:UnknownType;base64," +transportMessage.Body;
                            }
                            else
                            {
                                dataUri = null;
                            }
                            message = new Message(transportMessage.Headers, dataUri);
                        }
                    }

                    if (message.HasException())
                    {
                        var content = _serializer.Serialize(message);

                    }
                    else
                    { 
                        // client.Commit(sender);
                        var (stream, groupname, id) = ((string stream, string group, string id))sender;
                        message.Headers[Headers.StreamMessageId] = id;
                        _dispatcher.EnqueueToExecute(message, executor!);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An exception occurred when process received message. Message:'{0}'.", transportMessage);

                    client.Reject(sender);

                    TracingError(tracingTimestamp, transportMessage, e);
                }
            };

        }


        #region tracing

        private void TracingError(long? tracingTimestamp, TransportMessage message, Exception ex)
        {
           _logger.LogError(ex,$"timestamp: {tracingTimestamp} message: {message}  exception message: {ex.Message}");
        }

        #endregion
    }
}