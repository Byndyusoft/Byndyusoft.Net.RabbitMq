using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Topology;
using Byndyusoft.Messaging.Utils;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using RabbitMQ.Client.Exceptions;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitQueueServiceHandler : Disposable, IRabbitQueueServiceHandler
    {
        private readonly IBusFactory _busFactory;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly object _lock = new();
        private readonly Dictionary<string, IPullingConsumer<PullResult>> _pullingConsumers = new();
        private IBus _bus = default!;
        private bool _isInitialized;

        public RabbitQueueServiceHandler(string connectionString)
            : this(connectionString, new BusFactory())
        {
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));

            _connectionConfiguration = new ConnectionStringParser().Parse(connectionString);
        }

        public RabbitQueueServiceHandler(string connectionString, IBusFactory busFactory)
        {
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));
            Preconditions.CheckNotNull(busFactory, nameof(busFactory));

            _connectionConfiguration = new ConnectionStringParser().Parse(connectionString);
            _busFactory = busFactory;
        }

        public ConnectionConfiguration ConnectionConfiguration
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _connectionConfiguration;
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
                var consumedMessage = RabbitMessageConverter.CreateConsumedMessage(body, properties, info)!;
                var consumeResult = await onMessage(consumedMessage, CancellationToken.None)
                    .ConfigureAwait(false);

                return consumeResult switch
                {
                    ConsumeResult.Ack => AckStrategies.Ack,
                    ConsumeResult.Error => throw new Exception(),
                    ConsumeResult.RejectWithRequeue => AckStrategies.NackWithRequeue,
                    ConsumeResult.RejectWithoutRequeue => AckStrategies.NackWithoutRequeue,
                    _ => throw new ArgumentOutOfRangeException()
                };
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