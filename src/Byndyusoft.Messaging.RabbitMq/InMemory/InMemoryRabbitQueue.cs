using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitQueue : Disposable
    {
        private readonly object _lock = new();
        private readonly List<InMemoryRabbitQueueMessage> _messages = new();

        public InMemoryRabbitQueue(string name, QueueOptions options)
        {
            Name = name;
            Options = options;
        }

        public string Name { get; }

        public IEnumerable<InMemoryRabbitQueueMessage> Messages => _messages;

        internal List<InMemoryRabbitQueueConsumer> Consumers { get; } = new();

        public QueueOptions Options { get; }

        internal bool IsNotEmpty => _messages.Any();

        internal bool IsUsed => IsNotEmpty == false || Consumers.Any();

        public bool IsDeadLetter => Options.GetDeadLetterRoutingKey() is not null;

        internal ConsumedQueueMessage? Get(string? consumerTag = null)
        {
            Preconditions.CheckNotDisposed(this);

            if (Monitor.TryEnter(_lock) == false)
                return null;

            try
            {
                foreach (var message in _messages)
                    if (message.IsReady)
                        return message.Consume(Name, consumerTag);

                return null;
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        internal void Ack(ConsumedQueueMessage consumedMessage)
        {
            Preconditions.CheckNotDisposed(this);

            lock (_lock)
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    var message = _messages[i];
                    if (message.DeliveryTag == consumedMessage.DeliveryTag)
                    {
                        message.Dispose();
                        _messages.RemoveAt(i);
                    }
                }
            }
        }

        internal void Reject(ConsumedQueueMessage consumedMessage, bool requeue)
        {
            Preconditions.CheckNotDisposed(this);

            lock (_lock)
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    var message = _messages[i];
                    if (message.DeliveryTag == consumedMessage.DeliveryTag)
                    {
                        if (requeue)
                        {
                            message.Requeue();
                        }
                        else
                        {
                            message.Dispose();
                            _messages.RemoveAt(i);
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                lock (_lock)
                {
                    MultiDispose(_messages);
                    _messages.Clear();
                    MultiDispose(Consumers);
                    Consumers.Clear();
                }
        }

        public IDisposable Consume(ushort prefetchCount,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            var consumer = new InMemoryRabbitQueueConsumer(this, onMessage, prefetchCount);
            Consumers.Add(consumer);
            return consumer;
        }

        public override string ToString() => Name;

        internal void DoService(InMemoryRabbitQueueServiceHandler handler)
        {
            var messageTtl = Options.GetMessageTtl();
            var deadLetterQueueName = Options.GetDeadLetterRoutingKey();

            lock (_lock)
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    var message = _messages[i];
                    if (message.IsConsuming)
                        continue;
                    if (message.IsExpired())
                    {
                        message.Dispose();
                        _messages.RemoveAt(i--);
                        continue;
                    }

                    if (deadLetterQueueName is null)
                        continue;


                    var isExpired = messageTtl is not null && message.IsExpired(messageTtl!);
                    if (!isExpired)
                        continue;

                    var queue = handler.Queues.SingleOrDefault(q => string.Equals(q.Name, deadLetterQueueName));
                    message.RetryTo(queue);

                    _messages.RemoveAt(i--);
                }
            }
        }

        internal void Add(InMemoryRabbitQueueMessage message)
        {
            lock (_lock)
            {
                _messages.Add(message);
            }
        }
    }
}