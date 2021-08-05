﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <inheritdoc cref="IQueueService" />
    public sealed class QueueService : IQueueService
    {
        /// <summary>
        ///     Rabbit connections factory
        /// </summary>
        private readonly IBusFactory _busFactory;

        /// <summary>
        ///     Full rabbit connection and topology configuration
        /// </summary>
        private readonly RabbitMqConfiguration _configuration;

        /// <summary>
        ///     IoC service locator for getting wrappers and pipes
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     RabbitMq connection
        /// </summary>
        private IBus _bus;

        /// <summary>
        ///     True, if service has been already initialized
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        ///     Pipelines for consuming incoming messages
        /// </summary>
        private IDictionary<Type, QueuePipeline> _consumingPipelines { get; }

        /// <summary>
        ///     Pipelines for producing outgoing messages
        /// </summary>
        private IDictionary<Type, QueuePipeline> _producingPipelines { get; }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="busFactory">Rabbit connections factory</param>
        /// <param name="configuration">Full rabbit connection and topology configuration</param>
        /// <param name="serviceProvider">IoC service locator for getting wrappers and pipes</param>
        public QueueService(IBusFactory busFactory, RabbitMqConfiguration configuration, IServiceProvider serviceProvider)
        {
            _busFactory = busFactory ?? throw new ArgumentNullException(nameof(busFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumingPipelines = new Dictionary<Type, QueuePipeline>();
            _producingPipelines = new Dictionary<Type, QueuePipeline>();
        }

        /// <inheritdoc />
        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            ValidateConfig(_configuration);

            _bus = _busFactory.CreateBus(_configuration.ConnectionString);

            _bus.Advanced.MessageReturned += OnMessageReturned;

            foreach (var exchangeCfg in _configuration.ExchangeConfigurations.Values)
            {
                var exchange = await _bus.Advanced.ExchangeDeclareAsync(exchangeCfg.ExchangeName, ExchangeType.Direct)
                    .ConfigureAwait(false);

                foreach (var queueCfg in exchangeCfg.ConsumeQueueConfigurations)
                {
                    var queuePipeline = await BuildPipeline(exchangeCfg, queueCfg, exchange);
                    _consumingPipelines.Add(queueCfg.MessageType, queuePipeline);
                }

                foreach (var queueCfg in exchangeCfg.ProduceQueueConfigurations)
                {
                    var queuePipeline = await BuildPipeline(exchangeCfg, queueCfg, exchange);
                    _producingPipelines.Add(queueCfg.MessageType, queuePipeline);
                }

            }

            _isInitialized = true;
        }

        /// <summary>
        ///     Builds and return message pipeline via queue
        /// </summary>
        /// <param name="exchangeCfg">Exchange configuration TODO: can be removed using exchange.Name ?</param>
        /// <param name="queueCfg">Queue configuration</param>
        /// <param name="exchange">Exchange</param>
        private async Task<QueuePipeline> BuildPipeline(ExchangeConfiguration exchangeCfg, QueueConfiguration queueCfg, IExchange exchange)
        {
            var queue = await _bus.Advanced.QueueDeclareAsync($"{exchangeCfg.ExchangeName}.{queueCfg.RoutingKey}")
                .ConfigureAwait(false);

            var queuePipeline = new QueuePipeline(queueCfg.RoutingKey, queue, exchange);

            foreach (var produceWrapper in queueCfg.Wrapers)
            {
                var wrapper = _serviceProvider.GetRequiredService(produceWrapper);
                queuePipeline.ProcessWrappers.Add(wrapper);
            }

            foreach (var returnPipe in queueCfg.Pipes)
            {
                var pipe = _serviceProvider.GetRequiredService(returnPipe);
                queuePipeline.FailurePipes.Add(pipe);
            }

            return queuePipeline;
        }


        #region Validation

        /// <summary>
        ///     Validates topology, throws exception on incorrect settings
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateConfig(RabbitMqConfiguration configuration)
        {
            var errors = new StringBuilder();
            if (configuration.ExchangeConfigurations == null ||
                configuration.ExchangeConfigurations.Any() == false)
            {
                errors.AppendLine("Empty exchages configuration");
            }
            else
            {
                foreach (var exchangeCfg in configuration.ExchangeConfigurations)
                {
                    ValidateConfig(exchangeCfg.Key, exchangeCfg.Value, errors);
                }
            }

            var error = errors.ToString();
            if (string.IsNullOrEmpty(error) == false)
                throw new InvalidOperationException(error);
        }

        /// <summary> 
        ///     Validates exchange configuration
        /// </summary>
        private void ValidateConfig(
            string exchangeName,
            ExchangeConfiguration exchangeCfg,
            StringBuilder errors)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                errors.AppendLine("Empty exchange key");

            if (exchangeCfg == null)
            {
                errors.AppendLine("Empty exchange configuration");
                return;
            }

            if (string.IsNullOrWhiteSpace(exchangeCfg.ExchangeName))
                errors.AppendLine("Empty exchange name");

            if (exchangeCfg.ExchangeName != exchangeName)
            {
                errors.AppendLine($"Exchange key {exchangeName} doesn't match exchange name {exchangeCfg.ExchangeName}");
            }

            if (ContainsUnsupportedCharacters(exchangeCfg.ExchangeName))
            {
                errors.AppendLine($"Exchange name {exchangeCfg.ExchangeName} contains unsupported symbols");
            }

            if ((exchangeCfg.ConsumeQueueConfigurations == null ||
                 exchangeCfg.ConsumeQueueConfigurations.Any() == false) &&
                (exchangeCfg.ProduceQueueConfigurations == null ||
                 exchangeCfg.ProduceQueueConfigurations.Any() == false))
            {
                errors.AppendLine("Exchange contains neither consuming pipelines, not producing pipelines");
            }

            if (exchangeCfg.ConsumeQueueConfigurations != null)
            {
                foreach (var queue in exchangeCfg.ConsumeQueueConfigurations)
                {
                    ValidateConfig(queue, errors);

                    if (exchangeCfg.ProduceQueueConfigurations != null)
                    {
                        if (exchangeCfg.ProduceQueueConfigurations.Any(cfg =>
                            string.Equals(cfg.RoutingKey.Trim(), queue.RoutingKey.Trim(),
                                StringComparison.InvariantCultureIgnoreCase)))
                        {
                            errors.AppendLine(
                                $"Queue routing key {queue.RoutingKey} is configured for consuming and for producing simultaneously");
                        }
                    }
                }
            }

            if (exchangeCfg.ProduceQueueConfigurations != null)
            {
                foreach (var queue in exchangeCfg.ProduceQueueConfigurations)
                {
                    ValidateConfig(queue, errors);
                    if (exchangeCfg.ConsumeQueueConfigurations != null)
                    {
                        if (exchangeCfg.ConsumeQueueConfigurations.Any(cfg =>
                            string.Equals(cfg.RoutingKey.Trim(), queue.RoutingKey.Trim(),
                                StringComparison.InvariantCultureIgnoreCase)))
                        {
                            errors.AppendLine(
                                $"Queue routing key {queue.RoutingKey} is configured for consuming and for producing simultaneously");
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Validates queue configuration
        /// </summary>
        private void ValidateConfig(QueueConfiguration queueCfg, StringBuilder errors)
        {
            if (queueCfg == null)
            {
                errors.AppendLine("Empty queue configuration");
                return;
            }

            if (string.IsNullOrWhiteSpace(queueCfg.QueueName))
                errors.AppendLine("Empty queue name");

            if (string.IsNullOrWhiteSpace(queueCfg.RoutingKey))
                errors.AppendLine("Empty queue routing key");

            if (ContainsUnsupportedCharacters(queueCfg.QueueName))
            {
                errors.AppendLine($"Queue name {queueCfg.QueueName} contains unsupported symbols");
            }

            if (ContainsUnsupportedCharacters(queueCfg.RoutingKey))
            {
                errors.AppendLine($"Queue routing key {queueCfg.RoutingKey} contains unsupported symbols");
            }
        }

        /// <summary>
        ///     Return true whether name contains unsupported chacaracters
        /// </summary>
        private static bool ContainsUnsupportedCharacters(string name)
        {
            return name.ToCharArray().Where(x => x != '_' && x != '.').All(char.IsLetterOrDigit) == false;
        }

        #endregion

        private void OnMessageReturned(object sender, MessageReturnedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task Publish<TMessage>(TMessage message, Dictionary<string, string>? headers = null,
                                      Action<MessageReturnedEventArgs>? returnedHandled = null,
                                      CancellationToken cancellationToken = default) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage,
                                             CancellationToken cancellationToken = default)
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages(CancellationToken cancellationToken = default)
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ResendErrorMessages(string routingKey, CancellationToken cancellationToken = default)
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_bus != null)
            {
                _bus.Advanced.MessageReturned -= OnMessageReturned;
                _bus.Dispose();
            }

            _isInitialized = false;
        }
    }

    /// <summary>
    ///     Consuming or producing message pipeline
    /// </summary>
    public class QueuePipeline
    {
        /// <summary>
        ///     Queue routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     Target queue
        /// </summary>
        public IQueue Queue { get; }

        /// <summary>
        ///     Target exchange
        /// </summary>
        public IExchange Exchange { get; }

        /// <summary>
        ///     Message processing wrappers
        /// </summary>
        public IList<object> ProcessWrappers { get; }

        /// <summary>
        ///     Failure pipes for handling errors on message consuming or returned produced messages
        /// </summary>
        public IList<object> FailurePipes { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="routingKey">Queue routing key</param>
        /// <param name="queue">Target queue</param>
        /// <param name="exchange">Target exchange</param>
        public QueuePipeline(string routingKey, IQueue queue, IExchange exchange)
        {
            RoutingKey = routingKey;
            Queue = queue;
            Exchange = exchange;
            ProcessWrappers = new List<object>();
            FailurePipes = new List<object>();
        }
    }
}