using System;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqConsumerExtensions
    {
        public static RabbitMqConsumer Start(this RabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StartAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static RabbitMqConsumer Stop(this RabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StopAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static RabbitMqConsumer WithQueueBinding(this RabbitMqConsumer consumer,
            string exchangeName,
            string routingKey)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                await consumer.Client.BindQueueAsync(exchangeName, routingKey, consumer.QueueName, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static RabbitMqConsumer WithQueue(this RabbitMqConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return consumer.WithQueue(options);
        }

        public static RabbitMqConsumer WithRetryQueue(this RabbitMqConsumer consumer, TimeSpan delay,
            QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                var retryQueueName = consumer.Client.Options.NamingConventions.RetryQueueName(consumer.QueueName);
                options = options
                    .WithMessageTtl(delay)
                    .WithDeadLetterExchange(null)
                    .WithDeadLetterRoutingKey(consumer.QueueName);
                await consumer.Client.CreateQueueIfNotExistsAsync(retryQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static RabbitMqConsumer WithRetryQueue(this RabbitMqConsumer consumer,
            TimeSpan delay,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithRetryQueue(consumer, delay, options);
        }

        public static RabbitMqConsumer WithErrorQueue(this RabbitMqConsumer consumer, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                var errorQueueName = consumer.Client.Options.NamingConventions.ErrorQueueName(consumer.QueueName);
                await consumer.Client.CreateQueueIfNotExistsAsync(errorQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static RabbitMqConsumer WithErrorQueue(this RabbitMqConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithErrorQueue(consumer, options);
        }
    }
}