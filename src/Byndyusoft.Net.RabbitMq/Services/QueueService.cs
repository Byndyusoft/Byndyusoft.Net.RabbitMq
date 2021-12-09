using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.Abstractions;
using Byndyusoft.Net.RabbitMq.Models;
using EasyNetQ;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Byndyusoft.Net.RabbitMq.Services
{
    /// <summary>
    ///     RabbitMq messaging service
    /// </summary>
    public sealed class QueueService : IHostedService, IQueueSubscriber, IMessagePublisher, IMessageResender
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
        private IBus? _bus;

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
        ///     Pipelines for producing outgoing messages
        /// </summary>
        private readonly IDictionary<Type, Delegate> _producingActions;

        /// <summary>
        ///     Pipelines for returned outgoing messages
        /// </summary>
        private readonly IDictionary<Type, Func<MessageReturnedEventArgs, Task>> _returnedActions;

        /// <summary>
        ///     Returns initialized bus connection
        /// </summary>
        private IBus Bus
        {
            get
            {
                if (_bus == null) throw new Exception("Queue service is not initialized");
                return _bus;
            }
        }


        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="busFactory">Rabbit connections factory</param>
        /// <param name="configuration">Full rabbit connection and topology configuration</param>
        /// <param name="serviceProvider">IoC service locator for getting wrappers and pipes</param>
        public QueueService(IBusFactory busFactory, 
                            RabbitMqConfiguration configuration,
                            IServiceProvider serviceProvider)
        {
            _busFactory = busFactory ?? throw new ArgumentNullException(nameof(busFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumingPipelines = new Dictionary<Type, QueuePipeline>();
            _producingPipelines = new Dictionary<Type, QueuePipeline>();
            _returnedActions = new Dictionary<Type, Func<MessageReturnedEventArgs, Task>>();
            _producingActions = new Dictionary<Type, Delegate>();
        }

        #region IHostedService Implementation


        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            ValidateConfig(_configuration);

            _bus = _busFactory.CreateBus(_configuration);

            Bus.Advanced.MessageReturned += OnMessageReturned;
            Bus.Advanced.Conventions.ErrorQueueNamingConvention = info => $"{info.Exchange}.{info.RoutingKey}.error";
            Bus.Advanced.Conventions.ErrorExchangeNamingConvention = info => $"{info.Exchange}.error";

            foreach (var exchangeCfg in _configuration.ExchangeConfigurations.Values)
            {
                var exchange = await Bus.Advanced.ExchangeDeclareAsync(exchangeCfg.ExchangeName, ExchangeType.Direct);

                foreach (var queueCfg in exchangeCfg.ConsumeQueueConfigurations)
                {
                    var queuePipeline = await BuildConsumingPipeline(exchangeCfg, queueCfg, exchange);
                    _consumingPipelines.Add(queueCfg.MessageType, queuePipeline);
                }

                foreach (var queueCfg in exchangeCfg.ProduceQueueConfigurations)
                {
                    var queuePipeline = await BuildProducingPipeline(exchangeCfg, queueCfg, exchange);
                    _producingPipelines.Add(queueCfg.MessageType, queuePipeline);

                    PrepareProducePipeline(queueCfg);
                    PrepareReturnedPipeline(queueCfg);
                }

            }

            _isInitialized = true;
        }


        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_bus != null)
            {
                _bus.Advanced.MessageReturned -= OnMessageReturned;
                _bus.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Prepares pipeline for producing messages and save it to internal collections
        /// </summary>
        /// <param name="queueCfg">Queue to produce messages</param>
        private void PrepareProducePipeline(QueueConfiguration queueCfg)
        {
            var method =
                typeof(QueueService).GetMethod(nameof(GetPublishPipeline), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                throw new PipelineConfigurationException("Failed to prepare produce pipeline");

            var generic = method.MakeGenericMethod(queueCfg.MessageType);
            if (generic == null)
                throw new PipelineConfigurationException("Failed to prepare produce pipeline");

            var pipeline = generic.Invoke(this, null) as Delegate;
            if (pipeline == null)
                throw new PipelineConfigurationException("Failed to prepare produce pipeline");

            _producingActions.Add(queueCfg.MessageType, pipeline);
        }


        /// <summary>
        ///     Prepares pipeline for producing messages and save it to internal collections
        /// </summary>
        /// <param name="queueCfg">Queue to produce messages</param>
        private void PrepareReturnedPipeline(QueueConfiguration queueCfg)
        {
            var method =
                typeof(QueueService).GetMethod(nameof(GetReturnedPipeline), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                throw new PipelineConfigurationException("");

            var generic = method.MakeGenericMethod(queueCfg.MessageType);
            var pipeline = (Func<MessageReturnedEventArgs, Task>)generic.Invoke(this, null);
            _returnedActions.Add(queueCfg.MessageType, pipeline);
        }


        /// <summary>
        ///     Builds and return consuming message pipeline via queue
        /// </summary>
        /// <param name="exchangeCfg">Exchange configuration TODO: can be removed using exchange.Name ?</param>
        /// <param name="queueCfg">Queue configuration</param>
        /// <param name="exchange">Exchange</param>
        private async Task<QueuePipeline> BuildConsumingPipeline(ExchangeConfiguration exchangeCfg, QueueConfiguration queueCfg, IExchange exchange)
        {
            var queue = await Bus.Advanced.QueueDeclareAsync($"{exchangeCfg.ExchangeName}.{queueCfg.RoutingKey}");

            await Bus.Advanced.BindAsync(exchange, queue, queueCfg.RoutingKey);

            var queuePipeline = new QueuePipeline(queueCfg.RoutingKey, queue, exchange);

            foreach (var middleware in queueCfg.Middlewares)
            {
                queuePipeline.ProcessMiddlewares.Add(middleware);
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
            var queue = await Bus.Advanced.QueueDeclareAsync($"{exchangeCfg.ExchangeName}.{queueCfg.RoutingKey}");

            var queuePipeline = new QueuePipeline(queueCfg.RoutingKey, queue, exchange);

            foreach (var middleware in queueCfg.Middlewares)
            {
                queuePipeline.ProcessMiddlewares.Add(middleware);
            }

            foreach (var middleware in queueCfg.ReturnedMiddlewares)
            {
                queuePipeline.ReturnedMiddlewares.Add(middleware);
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
            if (configuration.ExchangeConfigurations.Any() == false)
            {
                errors.AppendLine("Empty exchanges configuration");
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

            if (exchangeCfg.ConsumeQueueConfigurations.Any() == false &&
                exchangeCfg.ProduceQueueConfigurations.Any() == false)
            {
                errors.AppendLine("Exchange contains neither consuming pipelines, not producing pipelines");
            }


            foreach (var queue in exchangeCfg.ConsumeQueueConfigurations)
            {
                ValidateConfig(queue, errors);

                if (exchangeCfg.ProduceQueueConfigurations.Any(cfg =>
                    string.Equals(cfg.RoutingKey.Trim(), queue.RoutingKey.Trim(),
                        StringComparison.InvariantCultureIgnoreCase)))
                {
                    errors.AppendLine(
                        $"Queue routing key {queue.RoutingKey} is configured for consuming and for producing simultaneously");
                }
            }



            foreach (var queue in exchangeCfg.ProduceQueueConfigurations)
            {
                ValidateConfig(queue, errors);

                if (exchangeCfg.ConsumeQueueConfigurations.Any(cfg =>
                    string.Equals(cfg.RoutingKey.Trim(), queue.RoutingKey.Trim(),
                        StringComparison.InvariantCultureIgnoreCase)))
                {
                    errors.AppendLine(
                        $"Queue routing key {queue.RoutingKey} is configured for consuming and for producing simultaneously");
                }
            }

        }

        /// <summary>
        ///     Validates queue configuration
        /// </summary>
        private void ValidateConfig(QueueConfiguration queueCfg, StringBuilder errors)
        {
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


        #endregion


        #region IMessagePublisher Implementation


        /// <inheritdoc />
        public async Task Publish<TMessage>(TMessage message,
            string key,
            Dictionary<string, string>? headers = null,
            Action<MessageReturnedEventArgs>? returnedHandled = null,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");
            
            var publishPipeline = (Func<IMessage<TMessage>, Task>)_producingActions[typeof(TMessage)];
            
            var properties = GetMessageProperties();
            properties.Headers.Add(Consts.MessageKeyHeader, key);
            if (headers != null)
                foreach (var header in headers)
                    properties.Headers.Add(header.Key, header.Value);

            await publishPipeline(new Message<TMessage>(message));
        }


        /// <summary>
        ///     Returns pipeline for producing message
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        private Func<IMessage<TMessage>, Task> GetPublishPipeline<TMessage>() where TMessage : class
        {
            if (_producingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for producing");

            if (pipeline == null)
                throw new InvalidOperationException($"Producing pipeline is not found for type {typeof(TMessage)}");

            var producingMiddlewares = GetProducingMiddlewares<TMessage>(pipeline.ProcessMiddlewares);
            var publishPipeline = BuildPublish(pipeline, producingMiddlewares.GetEnumerator());
            return publishPipeline;
        }


        /// <summary>
        ///     Returns ordered list of  middlewares for producing particular message 
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        /// <param name="middlewareCfg">List of producing middleware types</param>
        private List<IProduceMiddleware<TMessage>> GetProducingMiddlewares<TMessage>(IList<Type> middlewareCfg) where TMessage : class
        {
            var middlewares = _serviceProvider.GetServices<IProduceMiddleware<TMessage>>().ToArray();
            var result = new List<IProduceMiddleware<TMessage>>();
            foreach (var middlewareType in middlewareCfg)
            {
                var mws = middlewares.Where(mw => mw.GetType() == middlewareType).ToArray();
                if (mws.Length == 0)
                    throw new PipelineConfigurationException($"Producing middleware {middlewareType.Name} is not registered");

                if (mws.Length > 1)
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
                var current = wrappersEnumerator.Current;
                return async message => await current
                    .Handle(message, BuildPublish(pipeline, wrappersEnumerator));
            }

            return async message => await Bus.Advanced.PublishAsync(pipeline.Exchange, pipeline.RoutingKey, true, message);
        }


        /// <summary>
        ///     Handles returned message
        /// </summary>
        private async void OnMessageReturned(object? sender, MessageReturnedEventArgs args)
        {
            var messageType = Type.GetType(args.MessageProperties.Type);
            if (_returnedActions.TryGetValue(messageType, out var pipeline) == false)
                throw new InvalidOperationException($"Type {args.MessageProperties.Type} was not registered for handling returned messages");

            if (pipeline == null)
                throw new InvalidOperationException($"Pipeline for handling returned messages is not found for type {args.MessageProperties.Type}");

            await pipeline(args);
        }


        /// <summary>
        ///     Returns pipeline for handling returned message
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        private Func<MessageReturnedEventArgs, Task> GetReturnedPipeline<TMessage>() where TMessage : class
        {
            if (_producingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for producing");

            if (pipeline == null)
                throw new InvalidOperationException($"Producing pipeline is not found for type {typeof(TMessage)}");

            var returnedMiddlewares = GetReturnedMiddlewares<TMessage>(pipeline.ReturnedMiddlewares);
            var returnedPipeline = BuildReturnedHandling(returnedMiddlewares.GetEnumerator());
            return returnedPipeline;
        }


        /// <summary>
        ///     Returns ordered list of  middlewares for handling returned message
        /// </summary>
        /// <param name="middlewareCfg">List of returned middleware types</param>
        private List<IReturnedMiddleware<TMessage>> GetReturnedMiddlewares<TMessage>(IList<Type> middlewareCfg) where TMessage : class
        {
            var middlewares = _serviceProvider.GetServices<IReturnedMiddleware<TMessage>>().ToArray();
            var result = new List<IReturnedMiddleware<TMessage>>();
            foreach (var middlewareType in middlewareCfg)
            {
                var mws = middlewares.Where(mw => mw.GetType() == middlewareType).ToArray();
                if (mws.Length == 0)
                    throw new PipelineConfigurationException($"Producing middleware {middlewareType.Name} is not registered");

                if (mws.Length > 1)
                    throw new PipelineConfigurationException($"Producing middleware {middlewareType.Name} is registered twice");

                result.Add(mws[0]);
            }

            return result;
        }


        /// <summary>
        ///     Returns returned message delegate, that will chain all middlewares
        /// </summary>
        /// <typeparam name="TMessage">Publishing message type</typeparam>
        /// <param name="wrappersEnumerator">Middlewares enumerator</param>
        private Func<MessageReturnedEventArgs, Task> BuildReturnedHandling<TMessage>(
            IEnumerator<IReturnedMiddleware<TMessage>> wrappersEnumerator) where TMessage : class
        {
            if (wrappersEnumerator.MoveNext() && wrappersEnumerator.Current != null)
            {
                var current = wrappersEnumerator.Current;
                return async message => await current
                    .Handle(message, BuildReturnedHandling(wrappersEnumerator));
            }

            return message => Task.CompletedTask;
        }


        private static MessageProperties GetMessageProperties()
        {
            return new MessageProperties
            {
                ContentType = MediaTypeNames.Application.Json,
                DeliveryMode = 2
            };
        }

        #endregion


        #region IQueueSubscriber Implementation


        /// <inheritdoc />
        public void Subscribe<TMessage>(Func<TMessage, Task> processMessage,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            if (_isInitialized == false)
                throw new InvalidOperationException("Initialize bus before use");

            if (_consumingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for consuming");

            if (pipeline == null)
                throw new InvalidOperationException($"Consuming pipeline is not found for type {typeof(TMessage)}");


            var consumingMiddlewares = GetConsumingMiddlewares<TMessage>(pipeline.ProcessMiddlewares);
            var consumePipeline = BuildConsume(processMessage, consumingMiddlewares.GetEnumerator());

            Bus.Advanced.Consume<TMessage>(pipeline.Queue, (message, messageInfo) => consumePipeline(message));
        }


        /// <summary>
        ///     Returns ordered list of  middlewares for consuming particular message 
        /// </summary>
        /// <typeparam name="TMessage">Incoming message type</typeparam>
        /// <param name="middlewareCfg">List of consuming middleware types</param>
        private List<IConsumeMiddleware<TMessage>> GetConsumingMiddlewares<TMessage>(IList<Type> middlewareCfg) where TMessage : class
        {
            var middlewares = _serviceProvider.GetServices<IConsumeMiddleware<TMessage>>().ToArray();
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
        /// <param name="processMessage">Target message processing delegate</param>
        /// <param name="wrappersEnumerator">Middlewares enumerator</param>
        private Func<IMessage<TMessage>, Task> BuildConsume<TMessage>(
            Func<TMessage, Task> processMessage,
            IEnumerator<IConsumeMiddleware<TMessage>> wrappersEnumerator) where TMessage : class
        {
            if (wrappersEnumerator.MoveNext() && wrappersEnumerator.Current != null)
            {
                var current = wrappersEnumerator.Current;
                return async message => await current
                    .Handle(message, BuildConsume(processMessage, wrappersEnumerator));
            }

            return async message => await processMessage(message.Body);
        }
        

        #endregion


        #region IMessageResender Implementation
        

        /// <inheritdoc />
        public async Task ResendErrorMessages<TMessage>(CancellationToken cancellationToken = default)
        {
            if (_consumingPipelines.TryGetValue(typeof(TMessage), out var pipeline) == false)
                throw new InvalidOperationException($"Type {typeof(TMessage)} was not registered for consuming");

            if (pipeline == null)
                throw new InvalidOperationException($"Consuming pipeline is not found for type {typeof(TMessage)}");

            var queue = await Bus.Advanced
                    .QueueDeclareAsync($"{pipeline.Exchange.Name}.{pipeline.RoutingKey}.error");

            var messageCount = Bus.Advanced.MessageCount(queue);
            var jsonSerializer = new JsonSerializer();

            while (messageCount > 0)
            {
                var getResult = Bus.Advanced.Get<Error>(queue);

                if (getResult.MessageAvailable)
                {
                    var error = getResult.Message.Body;
                    var headers = error.BasicProperties.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());

                    var properties = GetMessageProperties();

                    foreach (var header in headers)
                        properties.Headers.Add(header.Key, header.Value);

                    var newMessage = (Message<TMessage>)jsonSerializer.BytesToMessage(typeof(Message<TMessage>), Encoding.UTF8.GetBytes(error.Message));
                    
                    await Bus.Advanced.PublishAsync(pipeline.Exchange, error.RoutingKey, true, newMessage);
                }

                messageCount--;
            }
        }

        #endregion
    }
}