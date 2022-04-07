using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public delegate Task BeforeRabbitQueueConsumerStartEventHandler(RabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public delegate Task AfterRabbitQueueConsumerStopEventHandler(RabbitMqConsumer consumer,
        CancellationToken cancellationToken);

    public class RabbitMqConsumer : Disposable
    {
        private readonly IRabbitMqClientHandler _handler;
        private readonly Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> _onMessage;
        private readonly List<BeforeRabbitQueueConsumerStartEventHandler> _onStartingActions = new();
        private readonly List<AfterRabbitQueueConsumerStopEventHandler> _onStoppedActions = new();
        private readonly string _queueName;
        private IDisposable? _consumer;
        private bool? _exclusive;
        private ushort? _prefetchCount;

        public RabbitMqConsumer(IRabbitMqClient service,
            IRabbitMqClientHandler handler,
            string queueName,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            QueueService = service;
            _handler = handler;
            _onMessage = onMessage;
            _queueName = queueName;
        }

        public bool IsRunning => _consumer is not null;

        public string QueueName
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _queueName;
            }
        }

        public bool? Exclusive
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _exclusive;
            }
            set
            {
                Preconditions.Check(IsRunning == false, "Can't change exclusive mode for started consumer");

                _exclusive = value;
            }
        }

        public ushort? PrefetchCount
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _prefetchCount;
            }
            set
            {
                Preconditions.Check(IsRunning == false, "Can't change prefetch count for started consumer");

                _prefetchCount = value;
            }
        }

        public IRabbitMqClient QueueService { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning)
                return;

            foreach (var action in _onStartingActions) await action(this, cancellationToken).ConfigureAwait(false);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                try
                {
                    try
                    {
                        var consumeResult = await _onMessage(message, token)
                            .ConfigureAwait(false);
                        return await HandleConsumeResultAsync(message, consumeResult, null, token)
                            .ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        return await HandleConsumeResultAsync(message, ConsumeResult.Error, exception, token)
                            .ConfigureAwait(false);
                    }
                }
                catch
                {
                    return ConsumeResult.RejectWithRequeue;
                }
            }

            _consumer = _handler.StartConsume(QueueName, _exclusive, _prefetchCount, OnMessage);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning == false)
                return;

            foreach (var action in _onStoppedActions) await action(this, cancellationToken).ConfigureAwait(false);

            _consumer?.Dispose();
            _consumer = null;
        }

        public RabbitMqConsumer WithQueue(QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            async Task EventHandler(RabbitMqConsumer _, CancellationToken cancellationToken)
            {
                await QueueService.CreateQueueIfNotExistsAsync(QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            _onStartingActions.Insert(0, EventHandler);
            return this;
        }

        public RabbitMqConsumer OnStarting(BeforeRabbitQueueConsumerStartEventHandler handler)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            _onStartingActions.Add(handler);
            return this;
        }

        public RabbitMqConsumer OnStopped(AfterRabbitQueueConsumerStopEventHandler handler)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            _onStoppedActions.Add(handler);
            return this;
        }

        /// <summary>Switch a consumer to exclusive mode</summary>
        public RabbitMqConsumer WithExclusive(bool exclusive)
        {
            Exclusive = exclusive;
            return this;
        }

        /// <summary>Sets prefetch count</summary>
        public RabbitMqConsumer WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
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

        private async Task<ConsumeResult> HandleConsumeResultAsync(ReceivedRabbitMqMessage consumedMessage,
            ConsumeResult consumeResult, Exception? exception, CancellationToken cancellationToken)
        {
            switch (consumeResult)
            {
                case ConsumeResult.Retry:
                    await _handler.PublishMessageToRetryQueueAsync(consumedMessage, cancellationToken)
                        .ConfigureAwait(false);
                    return ConsumeResult.Ack;
                case ConsumeResult.Error:
                    await _handler.PublishMessageToErrorQueueAsync(consumedMessage, exception, cancellationToken)
                        .ConfigureAwait(false);
                    return ConsumeResult.Ack;
                default:
                    return consumeResult;
            }
        }
    }
}