using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqConsumer : Disposable, IRabbitMqConsumer
    {
        private readonly List<(BeforeRabbitQueueConsumerStartDelegate Action, int Priority)>
            _beforeStartActions = new();

        private readonly IRabbitMqClientHandler _handler;
        private readonly string _queueName;
        private readonly RabbitMqClientCore _rabbitMqClientCore;

        private IDisposable? _consumer;
        private bool? _exclusive;
        private ReceivedRabbitMqMessageHandler _onMessage;
        private ushort? _prefetchCount;

        public RabbitMqConsumer(RabbitMqClientCore client,
            IRabbitMqClientHandler handler,
            string queueName,
            ReceivedRabbitMqMessageHandler onMessage)
        {
            _rabbitMqClientCore = client;
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

        public ReceivedRabbitMqMessageHandler OnMessage
        {
            get => _onMessage;
            set => _onMessage = Preconditions.CheckNotNull(value, nameof(OnMessage));
        }

        public void RegisterBeforeStartAction(BeforeRabbitQueueConsumerStartDelegate action, int priority)
        {
            _beforeStartActions.Add((action, priority));
        }

        public async Task<IRabbitMqConsumer> StartAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning)
                return this;

            await InvokeBeforeStartActionsAsync(cancellationToken).ConfigureAwait(false);

            async Task<HandlerConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                try
                {
                    try
                    {
                        var consumeResult = await _onMessage(message, token).ConfigureAwait(false);
                        return await _rabbitMqClientCore.ProcessConsumeResultAsync(message, consumeResult,
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        return await _rabbitMqClientCore.ProcessConsumeResultAsync(message,
                            ConsumeResult.Error(exception), cancellationToken);
                    }
                }
                catch
                {
                    return HandlerConsumeResult.RejectWithRequeue;
                }
            }

            _consumer = await _handler
                .StartConsumeAsync(QueueName, _exclusive, _prefetchCount, OnMessage, cancellationToken)
                .ConfigureAwait(false);

            return this;
        }

        public Task<IRabbitMqConsumer> StopAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning == false)
                return Task.FromResult<IRabbitMqConsumer>(this);

            _consumer?.Dispose();
            _consumer = null;

            return Task.FromResult<IRabbitMqConsumer>(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _consumer?.Dispose();
            _consumer = null;
        }

        private async Task InvokeBeforeStartActionsAsync(CancellationToken cancellationToken)
        {
            foreach (var (action, _) in _beforeStartActions.OrderBy(i => i.Priority))
                await action(this, cancellationToken).ConfigureAwait(false);
        }
    }
}