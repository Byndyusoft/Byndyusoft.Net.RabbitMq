using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Core.Messages;

namespace Byndyusoft.Messaging.RabbitMq.Core
{
    public static class RabbitMqConsumerRetryExtensions
    {
        public static IRabbitMqConsumer WithConstantTimeoutRetryStrategy(this IRabbitMqConsumer consumer, TimeSpan delay, int? maxRetryCount, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            QueueOptions options = QueueOptions.Default;
            optionsSetup.Invoke(options);

            return consumer.WithConstantTimeoutRetryStrategy(delay, maxRetryCount, options);
        }

        public static IRabbitMqConsumer WithConstantTimeoutRetryStrategy(this IRabbitMqConsumer consumer, TimeSpan delay, int? maxRetryCount, QueueOptions? options = null)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var retryQueueName = consumer.Client.Options.NamingConventions.RetryQueueName(consumer.QueueName);

            //Без локальной переменной будет рекурсия
            var onMessage = consumer.OnMessage;
            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
            {
                try
                {
                    Exception? handleException = null;
                    try
                    {
                        var result = await onMessage(message, cancellationToken);
                        if (result is not RetryConsumeResult)
                            return result;
                    }
                    catch (Exception exception)
                    {
                        handleException = exception;
                    }

                    if (maxRetryCount != null && message.RetryCount >= maxRetryCount)
                        return ConsumeResult.Error(handleException);

                    var retryMessage = RabbitMqMessageFactory.CreateRetryMessage(message, retryQueueName);
                    retryMessage.Headers.SetException(handleException);

                    await consumer.Client.PublishMessageAsync(retryMessage, cancellationToken).ConfigureAwait(false);
                    return ConsumeResult.Ack();
                }
                catch (Exception exception)
                {
                    return ConsumeResult.Error(exception);
                }
            }
            consumer.OnMessage = OnMessage;

            if (options is not null)
            {
                options = options
                    .WithMessageTtl(delay)
                    .WithDeadLetterExchange(null)
                    .WithDeadLetterRoutingKey(consumer.QueueName);

                consumer.WithDeclareQueue(retryQueueName, options);
            }

            return consumer;
        }
    }
}