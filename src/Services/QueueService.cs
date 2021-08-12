using System;
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
        private readonly IDictionary<Type, QueuePipeline> _consumingPipelines;

        /// <summary>
        ///     Pipelines for producing outgoing messages
        /// </summary>
        private readonly IDictionary<Type, QueuePipeline> _producingPipelines;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="busFactory">Rabbit connections factory</param>
        /// <param name="configuration">Full rabbit connection and topology configuration</param>
        /// <param name="serviceProvider">IoC service locator for getting wrappers and pipes</param>
        public QueueService(IBusFactory busFactory, RabbitMqConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _busFactory = busFactory ?? throw new ArgumentNullException(nameof(busFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumingPipelines = new Dictionary<Type, QueuePipeline>();
            _producingPipelines = new Dictionary<Type, QueuePipeline>();
        }

        #region Initialize

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
                    var queuePipeline = await BuildConsumingPipeline(exchangeCfg, queueCfg, exchange);
                    _consumingPipelines.Add(queueCfg.MessageType, queuePipeline);
                }

                foreach (var queueCfg in exchangeCfg.ProduceQueueConfigurations)
                {
                    var queuePipeline = await BuildProducingPipeline(exchangeCfg, queueCfg, exchange);
                    _producingPipelines.Add(queueCfg.MessageType, queuePipeline);
                }

            }

            _isInitialized = true;
        }

        /// <summary>
        ///     Builds and return consuming message pipeline via queue
        /// </summary>
        /// <param name="exchangeCfg">Exchange configuration TODO: can be removed using exchange.Name ?</param>
        /// <param name="queueCfg">Queue configuration</param>
        /// <param name="exchange">Exchange</param>
        private async Task<QueuePipeline> BuildConsumingPipeline(ExchangeConfiguration exchangeCfg, QueueConfiguration queueCfg, IExchange exchange)
        {
            var queue = await _bus.Advanced.QueueDeclareAsync($"{exchangeCfg.ExchangeName}.{queueCfg.RoutingKey}")
                .ConfigureAwait(false);

            var queuePipeline = new QueuePipeline(queueCfg.RoutingKey, queue, exchange);

            foreach (var produceWrapper in queueCfg.Middlewares)
            {
                queuePipeline.ProcessWrappers.Add(produceWrapper);
            }

            foreach (var returnPipe in queueCfg.Pipes)
            {
                queuePipeline.FailurePipes.Add(returnPipe);
            }

            return queuePipeline;
        }


        /// <summary>
        ///     Builds and return producing message pipeline via queue
        /// </summary>
        /// <param name="exchangeCfg">Exchange configuration TODO: can be removed using exchange.Name ?</param>
        /// <param name="queueCfg">Queue configuration</param>
        /// <param name="exchange">Exchange</param>
        private async Task<QueuePipeline> BuildProducingPipeline(ExchangeConfiguration exchangeCfg, QueueConfiguration queueCfg, IExchange exchange)
        {
            var queue = await _bus.Advanced.QueueDeclareAsync($"{exchangeCfg.ExchangeName}.{queueCfg.RoutingKey}")
                .ConfigureAwait(false);

            var queuePipeline = new QueuePipeline(queueCfg.RoutingKey, queue, exchange);

            foreach (var produceWrapper in queueCfg.Middlewares)
            {
                queuePipeline.ProcessWrappers.Add(produceWrapper);
            }

            foreach (var returnPipe in queueCfg.Pipes)
            {
                queuePipeline.FailurePipes.Add(returnPipe);
            }

            return queuePipeline;
        }        

        #endregion
        

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


        #region Publish

        private void OnMessageReturned(object sender, MessageReturnedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task Publish<TMessage>(TMessage message,
            string key,
            Dictionary<string, string>? headers = null,
            Action<MessageReturnedEventArgs>? returnedHandled = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");


            if (_producingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for producing");

            if (pipeline == null)
                throw new InvalidOperationException($"Producing pipeline is not found for type {typeof(TMessage)}");

            var properties = GetMessageProperties();
            properties.Headers.Add(Consts.MessageKeyHeader, key);
            if (headers != null)
                foreach (var header in headers)
                    properties.Headers.Add(header.Key, header.Value);

            var producingMiddlewares = GetProducingMiddlewares<TMessage>(pipeline.ProcessWrappers);
            var publishPipeline = BuildPublish<TMessage>(pipeline, producingMiddlewares.GetEnumerator());

            await publishPipeline(new Message<TMessage>(message)).ConfigureAwait(false);
        }

        /// <summary>
        ///     Returns ordered list of  middlewares for producing particular message 
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        /// <param name="middlewareCfg">List of producing middleware types</param>
        private List<IProduceMiddleware<TMessage>> GetProducingMiddlewares<TMessage>(IList<Type> middlewareCfg) where TMessage : class
        {
            var middlewares = _serviceProvider.GetServices<IProduceMiddleware<TMessage>>();
            var result = new List<IProduceMiddleware<TMessage>>();
            foreach (var middlewareType in middlewareCfg)
            {
                var mws = middlewares.Where(mw => mw.GetType() == middlewareType).ToArray();
                if (mws.Length == 0)
                    throw new PipelineConfigurationException($"Producing middleware {middlewareType.Name} is not registered");

                if(mws.Length > 1)
                    throw new PipelineConfigurationException($"Producing middleware {middlewareType.Name} is registered twice");

                result.Add(mws[0]);
            }

            return result;
        }

        /// <summary>
        ///     Returns publish message delegate, that will chain all middlewares
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        /// <param name="pipeline">Producing pipeline</param>
        /// <param name="wrappersEnumerator">Middlewares enumerator</param>
        /// <returns></returns>
        private Func<IMessage<TMessage>, Task> BuildPublish<TMessage>(
            QueuePipeline pipeline,
            IEnumerator<IProduceMiddleware<TMessage>> wrappersEnumerator) where TMessage : class
        {
            if (wrappersEnumerator.MoveNext() && wrappersEnumerator.Current != null)
            {
                return async message => await wrappersEnumerator.Current
                    .Handle(message, BuildPublish(pipeline, wrappersEnumerator))
                    .ConfigureAwait(false);
            }

            return async message =>

                //TODO включить паблиш конфермс
                await _bus.Advanced
                    .PublishAsync(pipeline.Exchange, pipeline.RoutingKey, true, message)
                    .ConfigureAwait(false);
        }

        private static MessageProperties GetMessageProperties()
        {
            return new MessageProperties
            {
                //TODO: Вернуть, когда перейдем на netcore или netstandard2.1 
                //ContentType = MediaTypeNames.Application.Json,
                ContentType = "application/json",
                DeliveryMode = 2
            };
        }

        #endregion


        #region Subscribe

        /// <inheritdoc />
        public void SubscribeAsync<TMessage>(Func<TMessage, Task> processMessage,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            if (_consumingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for consuming");

            if (pipeline == null)
                throw new InvalidOperationException($"Consuming pipeline is not found for type {typeof(TMessage)}");


            var consumingMiddlewares = GetConsumingMiddlewares<TMessage>(pipeline.ProcessWrappers);
            var consumePipeline = BuildConsume<TMessage>(pipeline, processMessage, consumingMiddlewares.GetEnumerator());

            _bus.Advanced.Consume<TMessage>(new Queue(pipeline.RoutingKey, false), (message, messageInfo) => consumePipeline(message));
        }

        /// <summary>
        ///     Returns ordered list of  middlewares for consuming particular message 
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="middlewareCfg">List of consuming middleware types</param>
        private List<IConsumeMiddleware<TMessage>> GetConsumingMiddlewares<TMessage>(IList<Type> middlewareCfg) where TMessage : class
        {
            var middlewares = _serviceProvider.GetServices<IConsumeMiddleware<TMessage>>();
            var result = new List<IConsumeMiddleware<TMessage>>();
            foreach (var middlewareType in middlewareCfg)
            {
                var mws = middlewares.Where(mw => mw.GetType() == middlewareType).ToArray();
                if (mws.Length == 0)
                    throw new PipelineConfigurationException($"Consuming middleware {middlewareType.Name} is not registered");

                if (mws.Length > 1)
                    throw new PipelineConfigurationException($"Consuming middleware {middlewareType.Name} is registered twice");

                result.Add(mws[0]);
            }

            return result;
        }

        /// <summary>
        ///     Returns subscribe message delegate, that will chain all consuming middlewares
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        /// <param name="pipeline">Producing pipeline</param>
        /// <param name="processMessage">Target message processing delegate</param>
        /// <param name="wrappersEnumerator">Middlewares enumerator</param>
        /// <returns></returns>
        private Func<IMessage<TMessage>, Task> BuildConsume<TMessage>(
            QueuePipeline pipeline,
            Func<TMessage, Task> processMessage,
            IEnumerator<IConsumeMiddleware<TMessage>> wrappersEnumerator) where TMessage : class
        {
            if (wrappersEnumerator.MoveNext() && wrappersEnumerator.Current != null)
            {
                return async message => await wrappersEnumerator.Current
                    .Handle(message, BuildConsume(pipeline, processMessage, wrappersEnumerator))
                    .ConfigureAwait(false);
            }

            return async message => await processMessage(message.Body).ConfigureAwait(false);
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

        #endregion
        

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
}