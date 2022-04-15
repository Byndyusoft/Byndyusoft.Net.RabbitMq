using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Core
{
    public class RabbitMqConsumer : Disposable, IRabbitMqConsumer
    {
        private readonly IRabbitMqClientHandler _handler;
        private readonly Func<ReceivedRabbitMqMessage, CancellationToken, Task<ClientConsumeResult>> _onMessage;
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


        public event BeforeRabbitQueueConsumerStartEventHandler? OnStarting;

        public event AfterRabbitQueueConsumerStopEventHandler? OnStopped;

        public IRabbitMqClient Client { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);

            if (IsRunning)
                return;

            if(OnStarting != null)
                await OnStarting(this, cancellationToken).ConfigureAwait(false);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                try
                {
                    try
                    {
                        var consumeResult = await _onMessage(message, token).ConfigureAwait(false);
                        return await HandleConsumeResultAsync(message, consumeResult, token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        await _handler.PublishMessageToErrorQueueAsync(message, Client.Options.NamingConventions, exception, cancellationToken)
                            .ConfigureAwait(false);
                        return ConsumeResult.Ack;
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

            if(OnStopped != null)
                await OnStopped(this, cancellationToken).ConfigureAwait(false);

            _consumer?.Dispose();
            _consumer = null;
        }

        protected override void DisposeCore()
        {
            _consumer?.Dispose();
            _consumer = null;

            base.DisposeCore();
        }

        private async Task<ConsumeResult> HandleConsumeResultAsync(ReceivedRabbitMqMessage consumedMessage,
            ClientConsumeResult consumeResult, CancellationToken cancellationToken)
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
                    await _handler.PublishMessageToErrorQueueAsync(consumedMessage, Client.Options.NamingConventions, null, cancellationToken)
                        .ConfigureAwait(false);
                    return ConsumeResult.Ack;
            }
        }
    }
}