using System;
using System.Net.Http.Formatting;
using System.Net.Http.Json.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Diagnostics;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public abstract class RabbitMqClientCore : Disposable, IRabbitMqClient
    {
        private readonly RabbitMqClientActivitySource _activitySource;
        private readonly bool _disposeHandler;
        private IRabbitMqClientHandler _handler;
        private RabbitMqRpcClient _rpcClient;
        private readonly RabbitMqClientOptions _options;

        static RabbitMqClientCore()
        {
            MediaTypeFormatterCollection.Default.Add(new JsonMediaTypeFormatter());
        }

        protected RabbitMqClientCore(IRabbitMqClientHandler handler, bool disposeHandler = false)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            var options = handler.Options;

            _handler = handler;
            _options = options;
            _handler.MessageReturned += OnMessageReturned;
            _handler.Blocked += OnBlocked;
            _handler.Unblocked += OnUnblocked;
            _activitySource = new RabbitMqClientActivitySource();
            _disposeHandler = disposeHandler;
            _rpcClient = new RabbitMqRpcClient(_handler, options);
        }

        private void OnUnblocked(object sender, EventArgs e)
        {
            Unblocked?.Invoke(sender, e);
        }

        private void OnBlocked(object sender, EventArgs e)
        {
            Blocked?.Invoke(this, e);
        }

        public RabbitMqClientOptions Options
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _options;
            }
        }

        public event EventHandler? Blocked;
        public event EventHandler? Unblocked;

        public async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var activity = _activitySource.Activities.StartGetMessage(_handler.Endpoint, queueName);
            return await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    var message = await _handler.GetMessageAsync(queueName, cancellationToken)
                        .ConfigureAwait(false);

                    RabbitMqClientEvents.OnMessageGot(message);
                    return message;
                });
        }

        public async Task CompleteMessageAsync(ReceivedRabbitMqMessage message, ConsumeResult consumeResult,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            var activity = _activitySource.Activities.StartCompleteMessage(_handler.Endpoint, message, consumeResult);
            await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    var handlerConsumeResult =
                        await ProcessConsumeResultAsync(message, consumeResult, cancellationToken);
                    await _handler.CompleteMessageAsync(message, handlerConsumeResult, cancellationToken)
                        .ConfigureAwait(false);
                });
        }

        public async Task PublishMessageAsync(RabbitMqMessage message,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            var activity = _activitySource.Activities.StartPublishMessage(_handler.Endpoint, message);
            await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    RabbitMqClientEvents.OnMessagePublishing(message);
                    await _handler.PublishMessageAsync(message, cancellationToken).ConfigureAwait(false);
                });
        }

        public async Task CreateQueueAsync(string queueName,
            QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.PurgeQueueAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName,
            bool ifUnused = false,
            bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.DeleteQueueAsync(queueName, ifUnused, ifEmpty, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ulong> GetQueueMessageCountAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.GetQueueMessageCountAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            return await _handler.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteExchangeAsync(string exchangeName,
            bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.DeleteExchangeAsync(exchangeName, ifUnused, cancellationToken).ConfigureAwait(false);
        }

        public async Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.BindQueueAsync(exchangeName, routingKey, queueName, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UnbindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.UnbindQueueAsync(exchangeName, routingKey, queueName, cancellationToken)
                .ConfigureAwait(false);
        }

        public IRabbitMqConsumer Subscribe(string queueName, ReceivedRabbitMqMessageHandler onMessage)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return new RabbitMqConsumer(this, queueName, onMessage);
        }

        public async Task<ReceivedRabbitMqMessage> MakeRpc(RabbitMqMessage message,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(message, nameof(message));

            var activity = _activitySource.Activities.StartRpc(_handler.Endpoint, message);
            return await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    RabbitMqClientEvents.OnMessagePublishing(message);
                    var response = await _rpcClient.MakeRpc(message, cancellationToken)
                        .ConfigureAwait(false);
                    RabbitMqClientEvents.OnMessageReplied(response);
                    return response;
                }).ConfigureAwait(false);
        }

        public IRabbitMqConsumer SubscribeRpc(string queueName, RabbitMqRpcHandler onMessage)
        {
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return _rpcClient.SubscribeRpc(this, queueName, onMessage);
        }

        public event ReturnedRabbitMqMessageHandler? MessageReturned;

        internal async Task<IDisposable> StartConsumerAsync(RabbitMqConsumer consumer,
            CancellationToken cancellationToken)
        {
            async Task<HandlerConsumeResult> OnMessageHandler(ReceivedRabbitMqMessage message, CancellationToken ct)
            {
                await Task.Yield();

                try
                {
                    var activity = _activitySource.Activities.StartConsume(_handler.Endpoint, message);
                    return await _activitySource.ExecuteAsync(activity, async () =>
                        {
                            RabbitMqClientEvents.OnMessageGot(message);
                            try
                            {
                                var consumeResult = await consumer.OnMessage(message, ct).ConfigureAwait(false);
                                RabbitMqClientEvents.OnMessageConsumed(consumeResult);
                                return await ProcessConsumeResultAsync(message, consumeResult, ct);
                            }
                            catch (Exception exception)
                            {
                                return await ProcessConsumeResultAsync(message, ConsumeResult.Error(exception),
                                    ct);
                            }
                        }
                    );
                }
                catch
                {
                    return HandlerConsumeResult.RejectWithRequeue;
                }
            }

            return await _handler
                .StartConsumeAsync(consumer.QueueName, consumer.Exclusive, consumer.PrefetchCount,
                    OnMessageHandler, cancellationToken)
                .ConfigureAwait(false);
        }

        protected virtual async Task<HandlerConsumeResult> ProcessConsumeResultAsync(
            ReceivedRabbitMqMessage message,
            ConsumeResult consumeResult,
            CancellationToken cancellationToken)
        {
            switch (consumeResult)
            {
                case AckConsumeResult:
                    return HandlerConsumeResult.Ack;
                case RejectWithRequeueConsumeResult:
                    return HandlerConsumeResult.RejectWithRequeue;
                case RejectWithoutRequeueConsumeResult:
                    return HandlerConsumeResult.RejectWithoutRequeue;
                case ErrorConsumeResult error:
                {
                    await _handler.PublishMessageToErrorQueueAsync(message, Options.NamingConventions,
                            error.Exception, cancellationToken)
                        .ConfigureAwait(false);
                    return HandlerConsumeResult.Ack;
                }
                case RetryConsumeResult:
                {
                    await _handler
                        .PublishMessageToRetryQueueAsync(message, Options.NamingConventions, cancellationToken)
                        .ConfigureAwait(false);
                    return HandlerConsumeResult.Ack;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(consumeResult), consumeResult, null);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            _handler.MessageReturned -= OnMessageReturned;
            _handler.Blocked -= OnBlocked;
            _handler.Unblocked -= OnUnblocked;

            if (_disposeHandler)
            {
                _handler.Dispose();
                _handler = null!;
            }

            _rpcClient.Dispose();
            _rpcClient = null!;
        }

        private async ValueTask OnMessageReturned(ReturnedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            var activity = _activitySource.Activities.StartReturnMessage(_handler.Endpoint, message);
            await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    RabbitMqClientEvents.OnMessageReturned(message);

                    var task = MessageReturned?.Invoke(message, cancellationToken);
                    if (task is not null)
                        await task.Value;
                }).ConfigureAwait(false);
        }
    }
}