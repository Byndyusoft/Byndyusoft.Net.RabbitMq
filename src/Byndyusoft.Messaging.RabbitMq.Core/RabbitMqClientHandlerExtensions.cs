using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Messages;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientHandlerExtensions
    {
        public static async Task CreateQueueIfNotExistAsync(this IRabbitMqClientHandler handler, string queueName,
            QueueOptions options, CancellationToken cancellationToken)
        {
            if (await handler.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false) == false)
                await handler.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task PublishMessageToRetryQueueAsync(this IRabbitMqClientHandler handler,
            ReceivedRabbitMqMessage message, QueueNamingConventions queueNamingConventions,
            CancellationToken cancellationToken)
        {
            var retryQueueName = queueNamingConventions.RetryQueueName(message.Queue);
            await handler.CreateQueueIfNotExistAsync(retryQueueName, QueueOptions.Default
                .AsDurable(true)
                .WithMessageTtl(TimeSpan.FromMinutes(1))
                .WithDeadLetterExchange(null)
                .WithDeadLetterRoutingKey(message.Queue), cancellationToken).ConfigureAwait(false);

            var retryMessage = RabbitMqMessageFactory.CreateRetryMessage(message, retryQueueName);
            await handler.PublishMessageAsync(retryMessage, cancellationToken).ConfigureAwait(false);
        }

        public static async Task PublishMessageToErrorQueueAsync(this IRabbitMqClientHandler handler,
            ReceivedRabbitMqMessage message, QueueNamingConventions queueNamingConventions, Exception? exception,
            CancellationToken cancellationToken)
        {
            var errorQueueName = queueNamingConventions.ErrorQueueName(message.Queue);
            await handler
                .CreateQueueIfNotExistAsync(errorQueueName, QueueOptions.Default.AsDurable(true), cancellationToken)
                .ConfigureAwait(false);

            var errorMessage = RabbitMqMessageFactory.CreateErrorMessage(message, errorQueueName, exception);
            await handler.PublishMessageAsync(errorMessage, cancellationToken).ConfigureAwait(false);
        }

        public static async Task CompleteMessageAsync(
            this IRabbitMqClientHandler handler,
            ReceivedRabbitMqMessage message,
            HandlerConsumeResult handlerConsumeResult,
            CancellationToken cancellationToken)
        {
            switch (handlerConsumeResult)
            {
                case HandlerConsumeResult.Ack:
                    await handler.AckMessageAsync(message, cancellationToken).ConfigureAwait(false);
                    break;
                case HandlerConsumeResult.RejectWithRequeue:
                    await handler.RejectMessageAsync(message, true, cancellationToken).ConfigureAwait(false);
                    break;
                case HandlerConsumeResult.RejectWithoutRequeue:
                    await handler.RejectMessageAsync(message, false, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(handlerConsumeResult), handlerConsumeResult, null);
            }

            if (message is PulledRabbitMqMessage pulledRabbitMqMessage)
            {
                pulledRabbitMqMessage.IsCompleted = true;
            }
        }
    }
}