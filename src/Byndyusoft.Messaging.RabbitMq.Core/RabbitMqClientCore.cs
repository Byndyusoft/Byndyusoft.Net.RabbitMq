using System;
using System.Diagnostics;
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
        private readonly bool _disposeHandler;
        internal readonly RabbitMqClientActivitySource ActivitySource;
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
            ActivitySource = new RabbitMqClientActivitySource(options.DiagnosticsOptions);
            _disposeHandler = disposeHandler;
        }

        public RabbitMqClientCoreOptions Options { get; }

        public async Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);

            var activity = ActivitySource.Activities.StartGetMessage(_handler.Endpoint, queueName);
            return await ActivitySource.ExecuteAsync(activity,
                async () =>
                {
                    var message = await _handler.GetMessageAsync(queueName, cancellationToken)
                        .ConfigureAwait(false);
                    SetConsumedMessageProperties(message);
                    ActivitySource.Events.MessageGot(activity, message);
                    return message;
                });
        }

        public async Task CompleteMessageAsync(ReceivedRabbitMqMessage message, ConsumeResult consumeResult,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            var activity = ActivitySource.Activities.StartCompleteMessage(_handler.Endpoint, message, consumeResult);
            await ActivitySource.ExecuteAsync(activity,
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
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);

            SetPublishingMessageProperties(message);

            var activity = ActivitySource.Activities.StartPublishMessage(_handler.Endpoint, message);
            await ActivitySource.ExecuteAsync(activity,
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
                var consumeResult = await onMessage(message, ct).ConfigureAwait(false);
                return consumeResult;
            }

            return new RabbitMqConsumer(this, _handler, queueName, OnMessage);
        }

        protected internal virtual async Task<HandlerConsumeResult> ProcessConsumeResultAsync(
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
                    var activity = Activity.Current;
                    ActivitySource.Events.MessageRepublishedToError(activity);
                    return HandlerConsumeResult.Ack;
                }
                case RetryConsumeResult:
                {
                    await _handler
                        .PublishMessageToRetryQueueAsync(message, Options.NamingConventions, cancellationToken)
                        .ConfigureAwait(false);
                    var activity = Activity.Current;
                    ActivitySource.Events.MessageRepublishedToRetry(activity);
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

            if (_disposeHandler)
            {
                _handler.Dispose();
                _handler = null!;
            }
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