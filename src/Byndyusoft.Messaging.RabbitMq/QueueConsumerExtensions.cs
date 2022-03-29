using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class QueueConsumerExtensions
    {
        public static IQueueConsumer WithBindingToExchange(this IQueueConsumer consumer, string? exchangeName,
            string routingKey)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var rabbitService = consumer.QueueService as IRabbitQueueService;

            rabbitService?.BindQueueAsync(exchangeName, routingKey, consumer.QueueName)
                .GetAwaiter().GetResult();

            return consumer;
        }

        public static IQueueConsumer WithRetryTimeout(this IQueueConsumer consumer, TimeSpan delay)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var queueService = consumer.QueueService as IRabbitQueueService;
            if (queueService is null)
                return consumer;

            var retryQueueName = queueService.Options.RetryQueueName(consumer.QueueName);
            queueService.CreateQueueAsync(retryQueueName, options =>
            {
                options
                    .AsDurable(true)
                    .WithDeadLetterExchange(null)
                    .WithDeadLetterRoutingKey(consumer.QueueName)
                    .WithMessageTtl(delay);
            }).GetAwaiter().GetResult();

            return consumer;
        }

        public static IQueueConsumer OnStarting(this IQueueConsumer consumer,
            Func<IQueueConsumer, IRabbitQueueService, CancellationToken, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.Check(consumer.IsRunning == false, "Can't change running consumer");

            consumer.BeforeStart += async (c, h, ct) =>
                await handler(c, (IRabbitQueueService) h, ct).ConfigureAwait(false);
            return consumer;
        }

        public static IQueueConsumer OnStarting(this IQueueConsumer consumer,
            Func<IQueueConsumer, IRabbitQueueService, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));

            return consumer.OnStarting(async (c, h, _) => await handler(c, h).ConfigureAwait(false));
        }

        public static IQueueConsumer OnStopped(this IQueueConsumer consumer,
            Func<IQueueConsumer, IRabbitQueueService, CancellationToken, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.Check(consumer.IsRunning == false, "Can't change running consumer");

            consumer.AfterStop += async (c, h, ct) =>
                await handler(c, (IRabbitQueueService) h, ct).ConfigureAwait(false);
            return consumer;
        }

        public static IQueueConsumer OnStopped(this IQueueConsumer consumer,
            Func<IQueueConsumer, IRabbitQueueService, Task> handler)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(handler, nameof(handler));

            consumer.OnStopped(async (c, h, _) => await handler(c, h).ConfigureAwait(false));
            return consumer;
        }
    }
}