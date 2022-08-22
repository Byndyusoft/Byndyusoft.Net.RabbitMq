using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
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

        static RabbitMqClientCore()
        {
            MediaTypeFormatterCollection.Default.Add(new JsonMediaTypeFormatter());
        }

        protected RabbitMqClientCore(IRabbitMqClientHandler handler, RabbitMqClientCoreOptions options,
            bool disposeHandler = false)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.CheckNotNull(options, nameof(options));

            Options = options;
            _handler = handler;
            _activitySource = new RabbitMqClientActivitySource(options.DiagnosticsOptions);
            _disposeHandler = disposeHandler;
        }

        public RabbitMqClientCoreOptions Options { get; }

        public virtual async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var activity = _activitySource.Activities.StartGetMessage(_handler.Endpoint, queueName);
            return await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    var message = await _handler.GetMessageAsync(queueName, cancellationToken)
                        .ConfigureAwait(false);
                    SetConsumedMessageProperties(message);
                    _activitySource.Events.MessageGot(activity, message);
                    return message;
                });
        }

        public virtual async Task CompleteMessageAsync(ReceivedRabbitMqMessage message, ConsumeResult consumeResult,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            var activity = _activitySource.Activities.StartCompleteMessage(_handler.Endpoint, message, consumeResult);
            await _activitySource.ExecuteAsync(activity,
                async () =>
                {
                    switch (consumeResult)
                    {
                        case AckConsumeResult:
                            await _handler.AckMessageAsync(message, cancellationToken).ConfigureAwait(false);
                            break;
                        case RejectWithRequeueConsumeResult:
                            await _handler.RejectMessageAsync(message, true, cancellationToken).ConfigureAwait(false);
                            break;
                        case RejectWithoutRequeueConsumeResult:
                            await _handler.RejectMessageAsync(message, false, cancellationToken).ConfigureAwait(false);
                            break;
                        case ErrorConsumeResult error:
                            await _handler.PublishMessageToErrorQueueAsync(message, Options.NamingConventions,
                                    error.Exception, cancellationToken)
                                .ConfigureAwait(false);
                            await _handler.AckMessageAsync(message, cancellationToken).ConfigureAwait(false);
                            break;
                        //case RetryConsumeResult:
                        //    await _handler.PublishMessageToRetryQueueAsync(message, Options.NamingConventions, cancellationToken)
                        //        .ConfigureAwait(false);
                        //    await _handler.AckMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        //    break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(consumeResult), consumeResult, null);
                    }
                });
        }

        public virtual async Task PublishMessageAsync(RabbitMqMessage message,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            SetPublishingMessageProperties(message);

            var activity = _activitySource.Activities.StartPublishMessage(_handler.Endpoint, message);
            await _activitySource.ExecuteAsync(activity,
                async () => { await _handler.PublishMessageAsync(message, cancellationToken).ConfigureAwait(false); });
        }

        public async Task CreateQueueAsync(string queueName,
            QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.PurgeQueueAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName,
            bool ifUnused = false,
            bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.DeleteQueueAsync(queueName, ifUnused, ifEmpty, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ulong> GetQueueMessageCountAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.GetQueueMessageCountAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            return await _handler.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteExchangeAsync(string exchangeName,
            bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.DeleteExchangeAsync(exchangeName, ifUnused, cancellationToken).ConfigureAwait(false);
        }

        public async Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.BindQueueAsync(exchangeName, routingKey, queueName, cancellationToken)
                .ConfigureAwait(false);
        }

        public IRabbitMqConsumer Subscribe(string queueName,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken ct)
            {
                var activity = _activitySource.Activities.StartConsume(_handler.Endpoint, message);
                return await _activitySource.ExecuteAsync(activity,
                    async () =>
                    {
                        var consumeResult = await onMessage(message, ct).ConfigureAwait(false);
                        _activitySource.Events.MessageConsumed(activity, message, consumeResult);
                        return consumeResult;
                    });
            }

            return new RabbitMqConsumer(this, _handler, queueName, OnMessage);
        }


        protected override void DisposeCore()
        {
            if (_disposeHandler)
            {
                _handler.Dispose();
                _handler = null!;
            }

            base.DisposeCore();
        }

        protected void SetPublishingMessageProperties(RabbitMqMessage message)
        {
            message.Properties.ContentEncoding ??= message.Content.Headers.ContentEncoding?.FirstOrDefault();
            message.Properties.ContentType ??= message.Content.Headers.ContentType?.MediaType;
            message.Properties.AppId ??= Options.ApplicationName;
        }

        protected void SetConsumedMessageProperties(ReceivedRabbitMqMessage? message)
        {
            if (message is null)
                return;

            var properties = message.Properties;
            var content = message.Content;

            if (properties.ContentType is not null)
                content.Headers.ContentType = new MediaTypeHeaderValue(properties.ContentType);

            if (properties.ContentEncoding is not null)
                content.Headers.ContentEncoding.Add(properties.ContentEncoding);
        }
    }
}