// ReSharper disable CheckNamespace

using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqConsumerConstantTimeoutRetryStrategyExtensions
    {
        public static IRabbitMqConsumer WithConstantTimeoutRetryStrategy(this IRabbitMqConsumer consumer,
            TimeSpan delay, ulong? maxRetryCount, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup.Invoke(options);

            return consumer.WithConstantTimeoutRetryStrategy(delay, maxRetryCount, options);
        }

        public static IRabbitMqConsumer WithConstantTimeoutRetryStrategy(this IRabbitMqConsumer consumer,
            TimeSpan delay, ulong? maxRetryCount, QueueOptions? retryQueueOptions = null)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            //Без локальной переменной будет рекурсия
            var onMessage = consumer.OnMessage;

            consumer.OnMessage = OnMessage;

            retryQueueOptions =
                (retryQueueOptions ?? QueueOptions.Default)
                .WithMessageTtl(delay)
                .WithDeadLetterExchange(null)
                .WithDeadLetterRoutingKey(consumer.QueueName);
            var retryQueueName =
                consumer.Client.Options.NamingConventions.RetryQueueName(consumer.QueueName);
            consumer.WithDeclareQueue(retryQueueName, retryQueueOptions);

            return consumer;

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
            {
                try
                {
                    Exception? handledException = null;

                    try
                    {
                        var result = await onMessage(message, cancellationToken).ConfigureAwait(false);
                        if (result is not RetryConsumeResult)
                            return result;
                    }
                    catch (Exception exception)
                    {
                        handledException = exception;
                    }

                    if (maxRetryCount != null && message.RetryCount >= maxRetryCount)
                        return ConsumeResult.Error(handledException);

                    return ConsumeResult.Retry;
                }
                catch (Exception exception)
                {
                    return ConsumeResult.Error(exception);
                }
            }
        }
    }
}