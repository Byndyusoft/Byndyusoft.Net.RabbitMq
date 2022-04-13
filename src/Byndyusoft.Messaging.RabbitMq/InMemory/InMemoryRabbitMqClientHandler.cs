using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Microsoft.Extensions.Options;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitMqClientHandler : Disposable, IRabbitMqClientHandler
    {
        private readonly RabbitMqEndpoint _endpoint;
        private readonly Dictionary<string, InMemoryRabbitMqExchange> _exchanges = new();
        private readonly object _lock = new();
        private readonly RabbitMqClientOptions _options;
        private readonly Dictionary<string, InMemoryRabbitMqQueue> _queues = new();
        private readonly Timer _timer;

        public InMemoryRabbitMqClientHandler()
            : this(new RabbitMqClientOptions())
        {
        }

        public InMemoryRabbitMqClientHandler(RabbitMqClientOptions options)
            : this(Microsoft.Extensions.Options.Options.Create(options))
        {
        }

        public InMemoryRabbitMqClientHandler(IOptions<RabbitMqClientOptions> options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            _options = options.Value;
            _timer = new Timer(DoService, null, 0, 1000);
            _endpoint = new RabbitMqEndpoint {Host = "in-memory"};
        }

        public IEnumerable<InMemoryRabbitMqQueue> Queues
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _queues.Values;
            }
        }

        public IEnumerable<InMemoryRabbitMqExchange> Exchanges
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _exchanges.Values;
            }
        }

        public RabbitMqEndpoint Endpoint
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _endpoint;
            }
        }

        public RabbitMqClientOptions Options
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _options;
            }
        }

        public Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(queueName);
            var consumedMessage = queue.Get();
            return Task.FromResult(consumedMessage);
        }

        public Task AckMessageAsync(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(message.Queue);
            queue.Ack(message);
            return Task.CompletedTask;
        }

        public Task RejectMessageAsync(ReceivedRabbitMqMessage message, bool requeue,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(message.Queue);
            queue.Reject(message, requeue);
            return Task.CompletedTask;
        }

        public Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            if (message.Exchange is not null)
            {
                var exchange = GetRequiredExchange(message.Exchange);
                foreach (var (routingKey, queueName) in exchange.Bindings)
                    if (string.Equals(routingKey, message.RoutingKey))
                        PublishToQueue(queueName, message);
            }
            else
            {
                PublishToQueue(message.RoutingKey, message);
            }

            return Task.CompletedTask;
        }

        public IDisposable StartConsume(string queueName,
            bool? exclusive,
            ushort? prefetchCount,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(queueName);

            return queue.Consume(prefetchCount ?? 1, onMessage);
        }

        public Task CreateQueueAsync(string queueName,
            QueueOptions options,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            if (_queues.ContainsKey(queueName) == false)
            {
                var queue = new InMemoryRabbitMqQueue(queueName, options);
                _queues.Add(queueName, queue);
            }

            return Task.CompletedTask;
        }


        public Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var result = _queues.ContainsKey(queueName);
            return Task.FromResult(result);
        }

        public Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            if (_queues.TryGetValue(queueName, out var queue))
            {
                if (ifUnused && queue.IsUsed)
                    return Task.CompletedTask;
                if (ifEmpty && queue.IsNotEmpty)
                    return Task.CompletedTask;

                queue.Dispose();
                _queues.Remove(queueName);
            }

            return Task.CompletedTask;
        }

        public Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(queueName);
            queue.Clear();

            return Task.CompletedTask;
        }

        public Task<ulong> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var queue = GetRequiredQueue(queueName);
            return Task.FromResult((ulong) queue.Messages.Count());
        }

        public Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            if (_exchanges.ContainsKey(exchangeName) == false)
            {
                var queue = new InMemoryRabbitMqExchange(exchangeName, options);
                _exchanges.Add(exchangeName, queue);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var result = _exchanges.ContainsKey(exchangeName);
            return Task.FromResult(result);
        }

        public Task DeleteExchangeAsync(string exchangeName, bool ifUnused, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            _exchanges.Remove(exchangeName);
            return Task.CompletedTask;
        }

        public Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken)
        {
            Preconditions.CheckNotDisposed(this);

            var exchange = GetRequiredExchange(exchangeName);
            exchange.Bind(routingKey, queueName);
            return Task.CompletedTask;
        }

        public void Clear()
        {
            foreach (var queue in Queues) queue.Clear();
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();

            MultiDispose(_queues.Values);
            _queues.Clear();
            _exchanges.Clear();
            _timer.Dispose();
        }

        private InMemoryRabbitMqQueue GetRequiredQueue(string queueName)
        {
            if (_queues.TryGetValue(queueName, out var queue))
                return queue;
            throw new InMemoryRabbitMqException($"Queue with name '{queueName}' is not found");
        }

        private InMemoryRabbitMqExchange GetRequiredExchange(string exchangeName)
        {
            if (_exchanges.TryGetValue(exchangeName, out var exchange))
                return exchange;
            throw new InMemoryRabbitMqException($"Queue with name '{exchangeName}' is not found");
        }

        private void PublishToQueue(string queueName, RabbitMqMessage message)
        {
            if (_queues.TryGetValue(queueName, out var queue) == false)
                return;
            queue.Add(new InMemoryRabbitMqMessage(message));
        }

        private void DoService(object? state)
        {
            if (Monitor.TryEnter(_lock) == false)
                return;

            try
            {
                foreach (var queue in _queues.Values) queue.DoService(this);
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }
    }
}