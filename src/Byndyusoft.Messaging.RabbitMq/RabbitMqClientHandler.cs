using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client.Exceptions;
using ExchangeType = Byndyusoft.Messaging.RabbitMq.Topology.ExchangeType;
using QueueType = Byndyusoft.Messaging.RabbitMq.Topology.QueueType;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqClientHandler : Disposable, IRabbitMqClientHandler
    {
        private static readonly ConnectionStringParser ConnectionStringParser = new();
        private readonly IBusFactory _busFactory;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly ILogger<RabbitMqClientHandler> _logger;
        private readonly ConcurrentDictionary<string, IPullingConsumer<PullResult>> _pullingConsumers = new();
        private IBus? _bus;
        private RabbitMqEndpoint? _endPoint;
        private SemaphoreSlim? _mutex = new(1, 1);
        
        public RabbitMqClientHandler(
            RabbitMqClientOptions options,
            IBusFactory? busFactory = null,
            ILogger<RabbitMqClientHandler>? logger = null)
        {
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotNull(options.ConnectionString, nameof(RabbitMqClientOptions.ConnectionString));
            Preconditions.CheckNotNull(busFactory, nameof(busFactory));

            _connectionConfiguration = ConnectionStringParser.Parse(options.ConnectionString);
            _busFactory = busFactory ?? new BusFactory();
            _logger = logger ?? NullLogger<RabbitMqClientHandler>.Instance;
            Options = options;
        }

        public RabbitMqClientOptions Options { get; }

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

        public virtual async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var pullingConsumer = await GetPullingConsumer(queueName, cancellationToken)
                .ConfigureAwait(false);
            var pullingResult = await pullingConsumer.PullAsync(cancellationToken)
                .ConfigureAwait(false);

            return ReceivedRabbitMqMessageFactory.CreatePulledMessage(pullingResult, this);
        }

        public virtual async Task AckMessageAsync(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue,
                nameof(message),
                $"Message must have not null {nameof(ReceivedRabbitMqMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            var pullingConsumer = await GetPullingConsumer(message.Queue, cancellationToken)
                .ConfigureAwait(false);
            await pullingConsumer.AckAsync(message.DeliveryTag, false, cancellationToken)
                .ConfigureAwait(false);

            if (message is PulledRabbitMqMessage pulledRabbitMqMessage) pulledRabbitMqMessage.IsCompleted = true;
        }

        public virtual async Task RejectMessageAsync(ReceivedRabbitMqMessage message, bool requeue,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(message.Queue,
                nameof(message),
                $"Message must have not null {nameof(ReceivedRabbitMqMessage.Queue)}");
            Preconditions.CheckNotDisposed(this);

            var pullingConsumer = await GetPullingConsumer(message.Queue, cancellationToken)
                .ConfigureAwait(false);
            await pullingConsumer.RejectAsync(message.DeliveryTag, false, requeue, cancellationToken)
                .ConfigureAwait(false);

            if (message is PulledRabbitMqMessage pulledRabbitMqMessage) pulledRabbitMqMessage.IsCompleted = true;
        }

        public virtual async Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            var exchange = message.Exchange is null ? Exchange.Default : new Exchange(message.Exchange);

            var (body, properties) =
                await RabbitMqMessageFactory.CreateEasyNetQMessageAsync(message).ConfigureAwait(false);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.PublishAsync(exchange,
                    message.RoutingKey,
                    message.Mandatory,
                    properties,
                    body,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private class ConsumerDisposable : IDisposable
        {
            private readonly IDisposable _wrapped;
            private readonly CancellationTokenSource _cancellationTokenSource;

            public ConsumerDisposable(IDisposable wrapped, CancellationTokenSource cancellationTokenSource)
            {
                _wrapped = wrapped;
                _cancellationTokenSource = cancellationTokenSource;
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _wrapped.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }

        public virtual async Task<IDisposable> StartConsumeAsync(string queueName,
            bool? exclusive,
            ushort? prefetchCount,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> onMessage,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotDisposed(this);

            var stoppingTokenSource = new CancellationTokenSource();
            var stoppingToken = stoppingTokenSource.Token;

            try
            {
                var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                    .ConfigureAwait(false);
                var advancedBusConsumer = advancedBus.Consume(new Queue(queueName), OnMessage, ConfigureConsumer);
                return new ConsumerDisposable(advancedBusConsumer, stoppingTokenSource);
            }
            catch
            {
               stoppingTokenSource.Dispose();
               throw;
            }
            async Task<AckStrategy> OnMessage(ReadOnlyMemory<byte> body, MessageProperties properties,
                MessageReceivedInfo info)
            {
                try
                {
                    await using var consumedMessage = ReceivedRabbitMqMessageFactory.CreateReceivedMessage(body, properties, info);
                    var consumeResult = await onMessage(consumedMessage, stoppingToken)
                        .ConfigureAwait(false);

                    return consumeResult switch
                    {
                        HandlerConsumeResult.RejectWithRequeue => AckStrategies.NackWithRequeue,
                        HandlerConsumeResult.RejectWithoutRequeue => AckStrategies.NackWithoutRequeue,
                        HandlerConsumeResult.Ack => AckStrategies.Ack,
                        _ => throw new InvalidOperationException(
                            $"Unexpected ConsumeResult Value={consumeResult}, Retry or Error value should be handled previously")
                    };
                }
                catch
                {
                    return AckStrategies.NackWithRequeue;
                }
            }

            void ConfigureConsumer(ISimpleConsumeConfiguration configuration)
            {
                if (exclusive is not null) configuration.WithExclusive(exclusive.Value);
                if (prefetchCount is not null) configuration.WithPrefetchCount(prefetchCount.Value);
            }
        }

        public virtual async Task CreateQueueAsync(string queueName,
            QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.QueueDeclareAsync(queueName, ConfigureQueue, cancellationToken)
                .ConfigureAwait(false);
            return;

            void ConfigureQueue(IQueueDeclareConfiguration config)
            {
                var queueType = options.Type switch
                {
                    QueueType.Classic => "classic",
                    QueueType.Quorum => "quorum",
                    QueueType.Stream => "stream",
                    _ => throw new ArgumentOutOfRangeException(nameof(QueueOptions.Type))
                };

                config.AsAutoDelete(options.AutoDelete)
                    .AsDurable(options.Durable)
                    .AsExclusive(options.Exclusive)
                    .WithQueueType(queueType);

                foreach (var argument in options.Arguments)
                    config.WithArgument(argument.Key, argument.Value);
            }
        }

        public virtual async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            try
            {
                var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                    .ConfigureAwait(false);
                await advancedBus.QueueDeclarePassiveAsync(queueName, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public virtual async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.QueuePurgeAsync(queueName, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName,
            bool ifUnused = false,
            bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.QueueDeleteAsync(queueName, ifUnused, ifEmpty, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async Task<ulong> GetQueueMessageCountAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            var stats = await advancedBus.GetQueueStatsAsync(queueName, cancellationToken)
                .ConfigureAwait(false);
            return stats.MessagesCount;
        }

        public virtual async Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.ExchangeDeclareAsync(exchangeName, ConfigureExchange, cancellationToken)
                .ConfigureAwait(false);
            return;

            void ConfigureExchange(IExchangeDeclareConfiguration config)
            {
                var exchangeType = options.Type switch
                {
                    ExchangeType.Direct => "direct",
                    ExchangeType.Headers => "headers",
                    ExchangeType.Fanout => "fanout",
                    ExchangeType.Topic => "topic",
                    _ => throw new ArgumentOutOfRangeException(nameof(ExchangeOptions.Type))
                };
                
                config.AsAutoDelete(options.AutoDelete)
                    .AsDurable(options.Durable)
                    .AsAutoDelete(options.AutoDelete)
                    .WithType(exchangeType);

                foreach (var argument in options.Arguments) config.WithArgument(argument.Key, argument.Value);
            }
        }

        public virtual async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            try
            {
                var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                    .ConfigureAwait(false);
                await advancedBus.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == 404)
            {
                return false;
            }
        }

        public virtual async Task DeleteExchangeAsync(string exchangeName,
            bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.ExchangeDeleteAsync(new Exchange(exchangeName), ifUnused, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            var exchange = new Exchange(exchangeName);
            var queue = new Queue(queueName);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, routingKey, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual async Task UnbindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotDisposed(this);

            var exchange = new Exchange(exchangeName);
            var queue = new Queue(queueName);
            var binding = new Binding<Queue>(exchange, queue, routingKey);

            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);
            await advancedBus.UnbindAsync(binding, cancellationToken)
                .ConfigureAwait(false);
        }

        public virtual event EventHandler? Blocked;
        
        public virtual event EventHandler? Unblocked;

        public virtual event ReturnedRabbitMqMessageHandler? MessageReturned;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            MultiDispose(_pullingConsumers.Values);
            _pullingConsumers.Clear();

            if (_bus != null)
            {
                _bus.Advanced.MessageReturned -= OnMessageReturned;
                _bus.Advanced.Blocked -= OnBlocked;
                _bus.Advanced.Unblocked -= OnUnblocked;
                _bus.Advanced.Dispose();
                _bus.Dispose();
                _bus = null;
            }

            _mutex?.Dispose();
            _mutex = null;
        }

        private async ValueTask<IPullingConsumer<PullResult>> GetPullingConsumer(string queueName,
            CancellationToken cancellationToken)
        {
            var advancedBus = await ConnectIfNeededAsync(cancellationToken)
                .ConfigureAwait(false);

            return _pullingConsumers.GetOrAdd(
                queueName,
                _ => advancedBus.CreatePullingConsumer(new Queue(queueName), false));
        }

        private async ValueTask<IAdvancedBus> ConnectIfNeededAsync(CancellationToken cancellationToken = default)
        {
            await _mutex!.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await ConnectAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _mutex.Release();
            }

            return _bus!.Advanced;
        }

        private async ValueTask ConnectAsync(CancellationToken cancellationToken)
        {
            if (_bus is null)
            {
                _bus = _busFactory.CreateBus(Options, _connectionConfiguration);
                _bus.Advanced.MessageReturned += OnMessageReturned;
                _bus.Advanced.Blocked += OnBlocked;
                _bus.Advanced.Unblocked += OnUnblocked;
            }

            var advancedBus = _bus.Advanced;
            while (advancedBus.IsConnected == false && cancellationToken.IsCancellationRequested == false)
            {
                LogLevel logLevel;
                try
                {
                    await advancedBus.ConnectAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (advancedBus.IsConnected)
                        break;
                    logLevel = LogLevel.Information;
                }
                catch (BrokerUnreachableException)
                {
                    logLevel = LogLevel.Error;
                }

                var interval = _connectionConfiguration.ConnectIntervalAttempt;
                var message = $"None of the specified endpoints were reachable. Wait {interval} and try connect again";

                _logger.Log(logLevel, message);

                await Task.Delay(interval, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private void OnUnblocked(object sender, UnblockedEventArgs e)
        {
            Unblocked?.Invoke(this, e);
        }

        private void OnBlocked(object sender, BlockedEventArgs e)
        {
            Blocked?.Invoke(sender, e);
        }

        private async void OnMessageReturned(object sender, MessageReturnedEventArgs args)
        {
            await using var returnedMessage =
                ReceivedRabbitMqMessageFactory.CreateReturnedMessage(
                    args.MessageBody,
                    args.MessageProperties,
                    args.MessageReturnedInfo);

            try
            {
                var task = MessageReturned?.Invoke(returnedMessage, CancellationToken.None);
                if (task is not null)
                    await task.Value;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}