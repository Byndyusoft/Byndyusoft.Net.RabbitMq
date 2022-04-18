using System;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqConsumerExtensions
    {
        public static IRabbitMqConsumer Start(this IRabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StartAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static IRabbitMqConsumer Stop(this IRabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.StopAsync().GetAwaiter().GetResult();
            return consumer;
        }

        public static IRabbitMqConsumer WithQueueBinding(this IRabbitMqConsumer consumer,
            string exchangeName,
            string routingKey)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                await consumer.Client.BindQueueAsync(exchangeName, routingKey, consumer.QueueName, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithPrefetchCount(this IRabbitMqConsumer consumer,
            ushort prefetchCount)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.PrefetchCount = prefetchCount;

            return consumer;
        }

        public static IRabbitMqConsumer WithExclusive(this IRabbitMqConsumer consumer,
            bool exclusive)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.Exclusive = exclusive;

            return consumer;
        }

        public static IRabbitMqConsumer WithDeclareQueue(this IRabbitMqConsumer consumer, string queueName,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return consumer.WithDeclareQueue(queueName, options);
        }

        public static IRabbitMqConsumer WithDeclareQueue(this IRabbitMqConsumer consumer, string queueName,
            QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                await consumer.Client.CreateQueueIfNotExistsAsync(queueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithDeclareSubscribingQueue(this IRabbitMqConsumer consumer,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return consumer.WithDeclareSubscribingQueue(options);
        }

        public static IRabbitMqConsumer WithDeclareSubscribingQueue(this IRabbitMqConsumer consumer,
            QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                await consumer.Client.CreateQueueIfNotExistsAsync(consumer.QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithDeclareErrorQueue(this IRabbitMqConsumer consumer, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                var errorQueueName = consumer.Client.Options.NamingConventions.ErrorQueueName(consumer.QueueName);
                await consumer.Client.CreateQueueIfNotExistsAsync(errorQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithDeclareErrorQueue(this IRabbitMqConsumer consumer,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithDeclareErrorQueue(consumer, options);
        }
    }
}