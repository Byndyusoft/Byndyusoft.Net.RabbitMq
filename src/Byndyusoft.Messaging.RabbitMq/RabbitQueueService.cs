using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Core;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitQueueService : QueueService, IRabbitQueueService
    {
        private readonly IRabbitQueueServiceHandler _handler;

        public RabbitQueueService(string connectionString)
            : base(new RabbitQueueServiceHandler(connectionString), true)
        {
            _handler = (IRabbitQueueServiceHandler) Handler;
        }

        public RabbitQueueService(QueueServiceOptions options)
            : base(new RabbitQueueServiceHandler(options), true)
        {
            _handler = (IRabbitQueueServiceHandler) Handler;
        }

        public RabbitQueueService(IRabbitQueueServiceHandler handler)
            : base(handler)
        {
            _handler = handler;
        }

        public async Task CreateQueueAsync(string queueName, QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false,
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

        public async Task CreateExchangeAsync(string exchangeName, ExchangeOptions options,
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

        public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.DeleteExchangeAsync(exchangeName, ifUnused, cancellationToken).ConfigureAwait(false);
        }


        public async Task BindQueueAsync(string exchangeName, string routingKey, string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.BindQueueAsync(exchangeName, routingKey, queueName, cancellationToken)
                .ConfigureAwait(false);
        }

        public override IQueueConsumer Subscribe(string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken token)
            {
                try
                {
                    var consumeResult = await onMessage(message, token)
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

            return base.Subscribe(queueName, OnMessage);
        }

        private async Task<ConsumeResult> HandleConsumeResultAsync(ConsumedQueueMessage consumedMessage,
            ConsumeResult consumeResult, Exception? exception, CancellationToken cancellationToken)
        {
            if (consumeResult == ConsumeResult.Retry)
            {
                var retryQueueName = Options.RetryQueueName(consumedMessage.Queue);
                if (await _handler.QueueExistsAsync(retryQueueName, cancellationToken).ConfigureAwait(false) == false)
                    return ConsumeResult.RejectWithRequeue;

                var retryMessage = QueueMessageFactory.CreateRetryMessage(consumedMessage, retryQueueName);
                await _handler.PublishAsync(retryMessage, cancellationToken)
                    .ConfigureAwait(false);

                return ConsumeResult.Ack;
            }

            if (consumeResult == ConsumeResult.Error)
            {
                var errorQueueName = Options.ErrorQueueName(consumedMessage.Queue);
                if (await _handler.QueueExistsAsync(errorQueueName, cancellationToken).ConfigureAwait(false) == false)
                    return ConsumeResult.RejectWithRequeue;

                var errorMessage = QueueMessageFactory.CreateErrorMessage(consumedMessage, errorQueueName, exception);
                await _handler.PublishAsync(errorMessage, cancellationToken)
                    .ConfigureAwait(false);

                return ConsumeResult.Ack;
            }

            return consumeResult;
        }
    }
}