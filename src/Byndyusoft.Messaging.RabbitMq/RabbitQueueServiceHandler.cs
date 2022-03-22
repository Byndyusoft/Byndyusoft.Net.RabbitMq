using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Utils;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    internal class RabbitQueueServiceHandler : Disposable, IQueueServiceHandler
    {
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly object _lock = new();
        private readonly Dictionary<string, IPullingConsumer<PullResult>> _pullingConsumers = new();
        private IBus _bus = default!;
        private bool _isInitialized;

        public RabbitQueueServiceHandler(string connectionString)
        {
            Preconditions.CheckNotNull(connectionString, nameof(connectionString));

            _connectionConfiguration = new ConnectionStringParser().Parse(connectionString);
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

            void ConfigureConsumer(IConsumerConfiguration configuration)
            {
                if (exclusive is not null) configuration.WithExclusive(exclusive.Value);
                if (prefetchCount is not null) configuration.WithPrefetchCount(prefetchCount.Value);
            }

            var queue = new Queue(queueName);
            return _bus.Advanced.Consume(queue,
                async (body, properties, info) =>
                {
                    var consumedMessage = RabbitMessageConverter.CreateConsumedMessage(body, properties, info)!;
                    var consumeResult = await onMessage(consumedMessage, CancellationToken.None);

                    return consumeResult switch
                    {
                        ConsumeResult.Ack => AckStrategies.Ack,
                        ConsumeResult.Error => throw new Exception(),
                        ConsumeResult.RejectWithRequeue => AckStrategies.NackWithRequeue,
                        ConsumeResult.RejectWithoutRequeue => AckStrategies.NackWithoutRequeue,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }, ConfigureConsumer);
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _bus = RabbitHutch.CreateBus(_connectionConfiguration, _ => { });
            _isInitialized = true;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing == false)
                return;

            foreach (var pullingConsumer in _pullingConsumers.Values) pullingConsumer.Dispose();

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