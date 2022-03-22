using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Core
{
    internal class QueueConsumer : Disposable, IQueueConsumer
    {
        private readonly IQueueServiceHandler _handler;
        private readonly Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> _onMessage;
        private readonly string _queueName;
        private IDisposable? _consumer;
        private bool? _exclusive;
        private ushort? _prefetchCount;

        public QueueConsumer(IQueueServiceHandler handler, string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            _handler = handler;
            _onMessage = onMessage;
            _queueName = queueName;
        }

        private bool IsStarted => _consumer is not null;

        /// <inheritdoc />
        public string QueueName
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _queueName;
            }
        }

        /// <inheritdoc />
        public bool? Exclusive
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _exclusive;
            }
        }

        /// <inheritdoc />
        public ushort? PrefetchCount
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _prefetchCount;
            }
        }

        /// <inheritdoc />
        public ValueTask<IQueueConsumer> StartAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsStarted == false) _consumer = _handler.Consume(QueueName, _exclusive, _prefetchCount, OnMessage);

            return new ValueTask<IQueueConsumer>(this);
        }

        /// <inheritdoc />
        public ValueTask<IQueueConsumer> StopAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsStarted)
            {
                _consumer?.Dispose();
                _consumer = null;
            }

            return new ValueTask<IQueueConsumer>(this);
        }

        /// <inheritdoc />
        public IQueueConsumer WithExclusive(bool exclusive)
        {
            Preconditions.Check(IsStarted == false, "Can't change exclusive mode for started consumer");

            _exclusive = exclusive;
            return this;
        }

        /// <inheritdoc />
        public IQueueConsumer WithPrefetchCount(ushort prefetchCount)
        {
            Preconditions.Check(IsStarted == false, "Can't change prefetch count for started consumer");

            _prefetchCount = prefetchCount;
            return this;
        }

        private async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken cancellationToken)
        {
            var activity = QueueServiceActivitySource.StartConsume(_handler.ConnectionConfiguration, message);

            return await QueueServiceActivitySource.ExecuteAsync(activity,
                async () =>
                {
                    var result = await _onMessage(message, CancellationToken.None).ConfigureAwait(false);
                    QueueServiceActivitySource.MessageConsumed(activity, message, result);
                    return result;
                });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _consumer?.Dispose();
                _consumer = null;
            }

            base.Dispose(disposing);
        }
    }
}