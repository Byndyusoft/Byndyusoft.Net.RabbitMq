using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class QueueConsumerExtensions
    {
        public static IQueueConsumer WithQueueBinding(this IQueueConsumer consumer, string exchangeName,
            string routingKey)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                if (consumer.QueueService is not IRabbitQueueService rabbitService)
                    return;

                await rabbitService.BindQueueAsync(exchangeName, routingKey, consumer.QueueName, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static IQueueConsumer WithQueue(this IQueueConsumer consumer, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            async Task EventHandler(IQueueConsumer _, CancellationToken cancellationToken)
            {
                if (consumer.QueueService is not IRabbitQueueService rabbitService) return;

                await rabbitService.CreateQueueIfNotExistsAsync(consumer.QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            // should be invoked first
            var field = consumer.GetType().GetTypeInfo().GetDeclaredField(nameof(IQueueConsumer.BeforeStart));
            var current = (Delegate) field.GetValue(consumer);
            var dlg = Delegate.Combine(new BeforeQueueConsumerStartEventHandler(EventHandler), current);
            field.SetValue(consumer, dlg);

            return consumer;
        }

        public static IQueueConsumer WithQueue(this IQueueConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithQueue(consumer, options);
        }

        public static IQueueConsumer WithRetryQueue(this IQueueConsumer consumer, TimeSpan delay, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                if (consumer.QueueService is not IRabbitQueueService rabbitService)
                    return;

                var retryQueueName = rabbitService.Options.RetryQueueName(consumer.QueueName);
                options = options
                    .WithMessageTtl(delay)
                    .WithDeadLetterExchange(null)
                    .WithDeadLetterRoutingKey(consumer.QueueName);
                await rabbitService.CreateQueueIfNotExistsAsync(retryQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static IQueueConsumer WithRetryQueue(this IQueueConsumer consumer, TimeSpan delay,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithRetryQueue(consumer, delay, options);
        }

        public static IQueueConsumer WithErrorQueue(this IQueueConsumer consumer, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            return consumer.OnStarting(async (_, cancellationToken) =>
            {
                if (consumer.QueueService is not IRabbitQueueService rabbitService)
                    return;
                var errorQueueName = rabbitService.Options.ErrorQueueName(consumer.QueueName);
                await rabbitService.CreateQueueIfNotExistsAsync(errorQueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        public static IQueueConsumer WithErrorQueue(this IQueueConsumer consumer, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithErrorQueue(consumer, options);
        }
    }
}