using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Internal;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandler : Disposable, IRabbitMqClientHandler
    {
        private static readonly ConnectionStringParser ConnectionStringParser = new();
        private readonly IBusFactory _busFactory;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly object _lock = new();
        private readonly Dictionary<string, IPullingConsumer<PullResult>> _pullingConsumers = new();
        private IBus _bus = default!;
        private RabbitMqEndpoint? _endPoint;
        private bool _isInitialized;

        public RabbitMqClientHandler(IOptions<RabbitMqClientHandlerOptions> options, IBusFactory busFactory)
        {
            Preconditions.CheckNotNull(options.Value.ConnectionString, nameof(options.Value.ConnectionString));
            Preconditions.CheckNotNull(busFactory, nameof(busFactory));

            _connectionConfiguration = ConnectionStringParser.Parse(options.Value.ConnectionString);
            _busFactory = busFactory;
        }

        RabbitMqEndpoint IRabbitMqEndpointContainer.Endpoint
        {
            get
            {
                Preconditions.CheckNotDisposed(this);

                return _endPoint ??=
                    new RabbitMqEndpoint
                    {
                        Host = _connectionConfiguration.Hosts.FirstOrDefault()?.Host!,
                        Port = _connectionConfiguration.Hosts.FirstOrDefault()?.Port.ToString()
                    };
            }
        }

        public async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(queueName);
            var pullingResult = await pullingConsumer.PullAsync(cancellationToken)
                .ConfigureAwait(false);

            return ReceivedRabbitMqMessageFactory.CreateReceivedMessage(pullingResult);
        }

        public async Task AckMessageAsync(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue,
                nameof(message),
                $"Message must have not null {nameof(ReceivedRabbitMqMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(message.Queue);
            await pullingConsumer.AckAsync(message.DeliveryTag, false, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task RejectMessageAsync(ReceivedRabbitMqMessage message, bool requeue,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue,
                nameof(message),
                $"Message must have not null {nameof(ReceivedRabbitMqMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var pullingConsumer = GetPullingConsumer(message.Queue);
            await pullingConsumer.RejectAsync(message.DeliveryTag, false, requeue, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var exchange = message.Exchange is null ? Exchange.GetDefault() : new Exchange(message.Exchange);

            var (body, properties) =
                await RabbitMqMessageFactory.CreateEasyNetQMessageAsync(message).ConfigureAwait(false);

            await _bus.Advanced.PublishAsync(exchange,
                    message.RoutingKey,
                    message.Mandatory,
                    properties,
                    body,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public IDisposable StartConsume(string queueName,
            bool? exclusive,
            ushort? prefetchCount,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> onMessage)
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
                    var consumedMessage = ReceivedRabbitMqMessageFactory.CreateReceivedMessage(body, properties, info);
                    var consumeResult = await onMessage(consumedMessage, CancellationToken.None)
                        .ConfigureAwait(false);

                    return consumeResult switch
                    {
                        ConsumeResult.RejectWithRequeue => AckStrategies.NackWithRequeue,
                        ConsumeResult.RejectWithoutRequeue => AckStrategies.NackWithoutRequeue,
                        ConsumeResult.Ack => AckStrategies.Ack,
                        _ => throw new InvalidOperationException(
                            $"Unexpected ConsumeResult Value={consumeResult}, Retry or Error value should be handled previously")
                    };
                }
                catch
                {
                    return AckStrategies.NackWithRequeue;
                }
            }

            return _bus.Advanced.Consume(new Queue(queueName), OnMessage, ConfigureConsumer);
        }

        public async Task CreateQueueAsync(string queueName,
            QueueOptions options,
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

        public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            await _bus.Advanced.QueuePurgeAsync(new Queue(queueName), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName,
            bool ifUnused = false,
            bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            await _bus.Advanced.QueueDeleteAsync(new Queue(queueName), ifUnused, ifEmpty, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ulong> GetQueueMessageCountAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var starts = await _bus.Advanced.GetQueueStatsAsync(new Queue(queueName), cancellationToken)
                .ConfigureAwait(false);
            return starts.MessagesCount;
        }

        public async Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
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

        public async Task DeleteExchangeAsync(string exchangeName,
            bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            await _bus.Advanced.ExchangeDeleteAsync(new Exchange(exchangeName), ifUnused, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            Initialize();

            var exchange = new Exchange(exchangeName);
            var queue = new Queue(queueName);
            await _bus.Advanced.BindAsync(exchange, queue, routingKey, cancellationToken)
                .ConfigureAwait(false);
        }

        private void Initialize()
        {
            if (_isInitialized) 
                return;

            _bus = _busFactory.CreateBus(_connectionConfiguration);

            _isInitialized = true;
        }

        protected override void DisposeCore()
        {
            MultiDispose(_pullingConsumers.Values);

            _pullingConsumers.Clear();
            _bus.Dispose();

            base.DisposeCore();
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