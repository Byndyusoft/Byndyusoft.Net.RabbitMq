using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.Utils;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitQueueServiceHandler : Disposable, IRabbitQueueServiceHandler
    {
        private static readonly ConnectionStringParser ConnectionStringParser = new();

        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly IBusFactory _busFactory;
        private readonly object _lock = new();
        private readonly QueueServiceOptions _options;
        private readonly Dictionary<string, IPullingConsumer<PullResult>> _pullingConsumers = new();
        private QueueServiceEndpoint? _queueServiceEndpoint;
        private IBus _bus = default!;
        private bool _isInitialized;

        public RabbitQueueServiceHandler(string connectionString)
        {
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));

            _options = new QueueServiceOptions {ConnectionString = connectionString};
            _busFactory = new BusFactory();
            _connectionConfiguration = ConnectionStringParser.Parse(connectionString);
        }

        public RabbitQueueServiceHandler(QueueServiceOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            _options = options;
            _busFactory = new BusFactory();
            _connectionConfiguration = ConnectionStringParser.Parse(options.ConnectionString);
        }

        public RabbitQueueServiceHandler(IOptions<QueueServiceOptions> options, IBusFactory busFactory)
        {
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotNull(busFactory, nameof(busFactory));

            _options = options.Value;
            _connectionConfiguration = ConnectionStringParser.Parse(_options.ConnectionString);
            _busFactory = busFactory;
        }

        public QueueServiceOptions Options
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _options;
            }
        }

        public QueueServiceEndpoint QueueServiceEndpoint
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _queueServiceEndpoint ??=
                    new QueueServiceEndpoint
                    {
                        Transport = "amqp",
                        Host = _connectionConfiguration.Hosts.FirstOrDefault()?.Host!,
                        Port = _connectionConfiguration.Hosts.FirstOrDefault()?.Port.ToString()
                    };
            }
        }

        public async Task<ConsumedQueueMessage?> GetAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(queueName);
            var pullingResult = await pullingConsumer.PullAsync(cancellationToken)
                .ConfigureAwait(false);

            return RabbitMessageConverter.CreateConsumedMessage(pullingResult);
        }

        public async Task AckAsync(ConsumedQueueMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue, nameof(message),
                $"Message must have not null {nameof(ConsumedQueueMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(message.Queue);

            await pullingConsumer.AckAsync(message.DeliveryTag, false, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task RejectAsync(ConsumedQueueMessage message, bool requeue, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue, nameof(message),
                $"Message must have not null {nameof(ConsumedQueueMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(message.Queue);

            await pullingConsumer.RejectAsync(message.DeliveryTag, false, requeue, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PublishAsync(QueueMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var exchange = message.Exchange is null ? Exchange.GetDefault() : new Exchange(message.Exchange);

            var body = await RabbitMessageConverter.CreateRabbitMessageBodyAsync(message, cancellationToken)
                .ConfigureAwait(false);
            var properties = RabbitMessageConverter.CreateRabbitMessageProperties(message);

            await _bus.Advanced.PublishAsync(exchange, message.RoutingKey, message.Mandatory, properties, body,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PublishBatchAsync(IReadOnlyCollection<QueueMessage> messages,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(messages, nameof(messages));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            foreach (var message in messages)
                await PublishAsync(message, cancellationToken)
                    .ConfigureAwait(false);
        }

        public IDisposable Consume(string queueName, bool? exclusive, ushort? prefetchCount,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            void ConfigureConsumer(IConsumerConfiguration configuration)
            {
                if (exclusive is not null) configuration.WithExclusive(exclusive.Value);
                if (prefetchCount is not null) configuration.WithPrefetchCount(prefetchCount.Value);
            }

            async Task<AckStrategy> OnMessage(byte[] body, MessageProperties properties, MessageReceivedInfo info)
            {
                try
                {
                    var consumedMessage = RabbitMessageConverter.CreateConsumedMessage(body, properties, info)!;
                    var consumeResult = await onMessage(consumedMessage, CancellationToken.None)
                        .ConfigureAwait(false);
                    return await HandleConsumeResultAsync(body, properties, info, consumeResult)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return await HandleConsumeResultAsync(body, properties, info, ConsumeResult.Error, exception)
                        .ConfigureAwait(false);
                }
            }

            return _bus.Advanced.Consume(new Queue(queueName), OnMessage, ConfigureConsumer);
        }

        public async Task CreateQueueAsync(string queueName, QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            void ConfigureQueue(IQueueDeclareConfiguration config)
            {
                config.AsAutoDelete(options.AutoDelete)
                    .AsDurable(options.Durable)
                    .AsExclusive(options.Exclusive)
                    .WithQueueType(options.Type.ToString().ToLower());

                foreach (var argument in options.Arguments) config.WithArgument(argument.Key, argument.Value);
            }

            await _bus.Advanced.QueueDeclareAsync(queueName, ConfigureQueue, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            try
            {
                await _bus.Advanced.QueueDeclarePassiveAsync(queueName, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public async Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            await _bus.Advanced.QueueDeleteAsync(new Queue(queueName), ifUnused, ifEmpty, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CreateExchangeAsync(string exchangeName, ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            void ConfigureExchange(IExchangeDeclareConfiguration config)
            {
                config.AsAutoDelete(options.AutoDelete)
                    .AsDurable(options.Durable)
                    .AsAutoDelete(options.AutoDelete)
                    .WithType(options.Type.ToString().ToLower());

                foreach (var argument in options.Arguments) config.WithArgument(argument.Key, argument.Value);
            }

            await _bus.Advanced.ExchangeDeclareAsync(exchangeName, ConfigureExchange, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            try
            {
                await _bus.Advanced.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            await _bus.Advanced.ExchangeDeleteAsync(new Exchange(exchangeName), ifUnused, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task BindQueueAsync(string? exchangeName, string routingKey, string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var exchange = exchangeName != null ? new Exchange(exchangeName) : Exchange.GetDefault();
            var queue = new Queue(queueName);
            await _bus.Advanced.BindAsync(exchange, queue, routingKey, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<AckStrategy> HandleConsumeResultAsync(byte[] body, MessageProperties properties,
            MessageReceivedInfo info, ConsumeResult consumeResult, Exception? exception = null)
        {
            if (consumeResult == ConsumeResult.RejectWithRequeue)
                return AckStrategies.NackWithRequeue;
            if (consumeResult == ConsumeResult.RejectWithoutRequeue)
                return AckStrategies.NackWithoutRequeue;

            if (consumeResult == ConsumeResult.Retry)
            {
                var retryQueueName = _options.RetryQueueName(info.Queue);
                if (await QueueExistsAsync(retryQueueName).ConfigureAwait(false) == false)
                    return AckStrategies.NackWithRequeue;

                await _bus.Advanced.PublishAsync(Exchange.GetDefault(), retryQueueName, true, properties, body)
                    .ConfigureAwait(false);
            }

            if (consumeResult == ConsumeResult.Error)
            {
                var errorQueueName = _options.ErrorQueueName(info.Queue);

                if (await QueueExistsAsync(errorQueueName).ConfigureAwait(false) == false)
                    return AckStrategies.NackWithRequeue;

                if (exception is not null)
                {
                    properties.Headers["exception-type"] = exception.GetType().FullName;
                    properties.Headers["exception-message"] = exception.Message;
                }

                properties.Headers.Remove("x-death");
                properties.Headers.Remove("x-first-death-exchange");
                properties.Headers.Remove("x-first-death-queue");
                properties.Headers.Remove("x-first-death-reason");

                await _bus.Advanced.PublishAsync(Exchange.GetDefault(), errorQueueName, true, properties, body)
                    .ConfigureAwait(false);
            }

            return AckStrategies.Ack;
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _bus = _busFactory.CreateBus(_connectionConfiguration);

            _isInitialized = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing == false)
                return;

            MultiDispose(_pullingConsumers.Values);

            _pullingConsumers.Clear();
            _bus.Dispose();
        }

        private IPullingConsumer<PullResult> GetPullingConsumer(string queueName)
        {
            if (_pullingConsumers.TryGetValue(queueName, out var pullingConsumer) == false)
                lock (_lock)
                {
                    if (_pullingConsumers.TryGetValue(queueName, out pullingConsumer) == false)
                    {
                        pullingConsumer = _bus.Advanced.CreatePullingConsumer(new Queue(queueName), false);
                        _pullingConsumers.Add(queueName, pullingConsumer);
                    }
                }

            return pullingConsumer;
        }
    }
}