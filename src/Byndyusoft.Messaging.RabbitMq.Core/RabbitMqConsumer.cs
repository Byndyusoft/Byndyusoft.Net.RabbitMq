using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Core
{
    public class RabbitMqConsumer : Disposable, IRabbitMqConsumer
    {
        private readonly IRabbitMqClientHandler _handler;
        private readonly Func<ReceivedRabbitMqMessage, CancellationToken, Task<ClientConsumeResult>> _onMessage;
        private readonly List<BeforeRabbitQueueConsumerStartEventHandler> _onStartingActions = new();
        private readonly List<AfterRabbitQueueConsumerStopEventHandler> _onStoppedActions = new();
        private readonly string _queueName;
        private IDisposable? _consumer;
        private bool? _exclusive;
        private ushort? _prefetchCount;

        public RabbitMqConsumer(IRabbitMqClient client,
            IRabbitMqClientHandler handler,
            string queueName,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ClientConsumeResult>> onMessage)
        {
            Client = client;
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

        public IRabbitMqClient Client { get; }

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
                        var consumeResult = await _onMessage(message, token).ConfigureAwait(false);
                        return await HandleConsumeResultAsync(message, consumeResult, null, token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        return await HandleConsumeResultAsync(message, ClientConsumeResult.Error, exception, token).ConfigureAwait(false);
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

            foreach (var action in _onStoppedActions) 
                await action(this, cancellationToken).ConfigureAwait(false);

            _consumer?.Dispose();
            _consumer = null;
        }

        public IRabbitMqConsumer WithQueue(QueueOptions options)
        {
            Preconditions.CheckNotNull(options, nameof(options));

            async Task EventHandler(IRabbitMqConsumer _, CancellationToken cancellationToken)
            {
                await Client.CreateQueueIfNotExistsAsync(QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            _onStartingActions.Insert(0, EventHandler);
            return this;
        }

        public IRabbitMqConsumer OnStarting(BeforeRabbitQueueConsumerStartEventHandler handler)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            _onStartingActions.Add(handler);
            return this;
        }

        public IRabbitMqConsumer OnStopped(AfterRabbitQueueConsumerStopEventHandler handler)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            _onStoppedActions.Add(handler);
            return this;
        }

        /// <summary>Switch a consumer to exclusive mode</summary>
        public IRabbitMqConsumer WithExclusive(bool exclusive)
        {
            Exclusive = exclusive;
            return this;
        }

        /// <summary>Sets prefetch count</summary>
        public IRabbitMqConsumer WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        protected override void DisposeCore()
        {
            _consumer?.Dispose();
            _consumer = null;

            base.DisposeCore();
        }

        private async Task<ConsumeResult> HandleConsumeResultAsync(ReceivedRabbitMqMessage consumedMessage,
            ClientConsumeResult consumeResult, Exception? exception, CancellationToken cancellationToken)
        {
            switch (consumeResult)
            {


                case ClientConsumeResult.Ack:
                    return ConsumeResult.Ack;

                case ClientConsumeResult.RejectWithRequeue:
                    return ConsumeResult.RejectWithRequeue;

                case ClientConsumeResult.RejectWithoutRequeue:
                    return ConsumeResult.RejectWithoutRequeue;
                
                case ClientConsumeResult.Error:
                default:
                    await _handler.PublishMessageToErrorQueueAsync(consumedMessage, Client.Options.NamingConventions, exception, cancellationToken)
                        .ConfigureAwait(false);
                    return ConsumeResult.Ack;
            }
        }
    }
}