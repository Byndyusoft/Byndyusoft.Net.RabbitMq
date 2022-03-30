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
        private readonly IQueueService _service;
        private IDisposable? _consumer;
        private bool? _exclusive;
        private ushort? _prefetchCount;

        public QueueConsumer(IQueueService service, IQueueServiceHandler handler, string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            _service = service;
            _handler = handler;
            _onMessage = onMessage;
            _queueName = queueName;
        }

        public bool IsRunning => _consumer is not null;

        public event BeforeQueueConsumerStartEventHandler? BeforeStart;

        public event AfterQueueConsumerStopEventHandler? AfterStop;

        public IQueueService QueueService
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _service;
            }
        }

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
        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning == false)
            {
                await OnBeforeStartAsync(cancellationToken).ConfigureAwait(false);
                _consumer = _handler.Consume(QueueName, _exclusive, _prefetchCount, OnMessage);
            }
        }

        /// <inheritdoc />
        public async ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning)
            {
                await OnAfterStopAsync(cancellationToken).ConfigureAwait(false);
                _consumer?.Dispose();
                _consumer = null;
            }
        }

        /// <inheritdoc />
        public IQueueConsumer WithExclusive(bool exclusive)
        {
            Preconditions.Check(IsRunning == false, "Can't change exclusive mode for started consumer");

            _exclusive = exclusive;
            return this;
        }

        /// <inheritdoc />
        public IQueueConsumer WithPrefetchCount(ushort prefetchCount)
        {
            Preconditions.Check(IsRunning == false, "Can't change prefetch count for started consumer");

            _prefetchCount = prefetchCount;
            return this;
        }

        private async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken cancellationToken)
        {
            var activity = QueueServiceActivitySource.StartConsume(_handler, message);

            return await QueueServiceActivitySource.ExecuteAsync(activity,
                async () =>
                {
                    var consumeResult = await _onMessage(message, CancellationToken.None).ConfigureAwait(false);
                    QueueServiceActivitySource.MessageConsumed(activity, message, consumeResult);
                    return consumeResult;
                });
        }

        private async Task OnBeforeStartAsync(CancellationToken cancellationToken)
        {
            if (BeforeStart is not null)
                await BeforeStart(this, cancellationToken)
                    .ConfigureAwait(false);
        }

        private async Task OnAfterStopAsync(CancellationToken cancellationToken)
        {
            if (AfterStop is not null)
                await AfterStop(this, cancellationToken)
                    .ConfigureAwait(false);
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