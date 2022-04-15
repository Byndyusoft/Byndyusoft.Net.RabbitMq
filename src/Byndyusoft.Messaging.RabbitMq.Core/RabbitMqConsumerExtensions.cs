using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Core
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

            consumer.OnStarting += (async (_, cancellationToken) =>
            {
                await consumer.Client.BindQueueAsync(exchangeName, routingKey, consumer.QueueName, cancellationToken)
                    .ConfigureAwait(false);
            });

            return consumer;
        }

        public static IRabbitMqConsumer WithPrefetchCount(this IRabbitMqConsumer consumer,
            ushort prefetchCount)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.PrefetchCount = prefetchCount;

            return consumer;
        }
        
        public static IRabbitMqConsumer WithQueue(this IRabbitMqConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                await consumer.Client.CreateQueueIfNotExistsAsync(consumer.QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithRetryQueue(this IRabbitMqConsumer consumer, TimeSpan delay,
            QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                var retryQueueName = consumer.Client.Options.NamingConventions.RetryQueueName(consumer.QueueName);
                options = options
                    .WithMessageTtl(delay)
                    .WithDeadLetterExchange(null)
                    .WithDeadLetterRoutingKey(consumer.QueueName);
                await consumer.Client.CreateQueueIfNotExistsAsync(retryQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithRetryQueue(this IRabbitMqConsumer consumer,
            TimeSpan delay,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithRetryQueue(consumer, delay, options);
        }

        public static IRabbitMqConsumer WithErrorQueue(this IRabbitMqConsumer consumer, QueueOptions options)
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

        public static IRabbitMqConsumer WithErrorQueue(this IRabbitMqConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithErrorQueue(consumer, options);
        }
    }
}