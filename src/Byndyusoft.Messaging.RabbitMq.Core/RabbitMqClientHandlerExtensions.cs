using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Core.Messages;

namespace Byndyusoft.Messaging.RabbitMq.Core
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
            ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
        {
            var retryQueueName = handler.Options.NamingConventions.RetryQueueName(message.Queue);
            await handler.CreateQueueIfNotExistAsync(retryQueueName, QueueOptions.Default
                .AsDurable(true)
                .WithMessageTtl(TimeSpan.FromMinutes(1))
                .WithDeadLetterExchange(null)
                .WithDeadLetterRoutingKey(message.Queue), cancellationToken).ConfigureAwait(false);

            var retryMessage = RabbitMqMessageFactory.CreateRetryMessage(message, retryQueueName);
            await handler.PublishMessageAsync(retryMessage, cancellationToken).ConfigureAwait(false);
        }

        public static async Task PublishMessageToErrorQueueAsync(this IRabbitMqClientHandler handler,
            ReceivedRabbitMqMessage message, Exception? exception, CancellationToken cancellationToken)
        {
            var errorQueueName = handler.Options.NamingConventions.ErrorQueueName(message.Queue);
            await handler
                .CreateQueueIfNotExistAsync(errorQueueName, QueueOptions.Default.AsDurable(true), cancellationToken)
                .ConfigureAwait(false);

            var errorMessage = RabbitMqMessageFactory.CreateErrorMessage(message, errorQueueName, exception);
            await handler.PublishMessageAsync(errorMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}