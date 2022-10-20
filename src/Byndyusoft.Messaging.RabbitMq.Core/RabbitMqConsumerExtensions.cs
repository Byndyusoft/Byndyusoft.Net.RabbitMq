using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqConsumerExtensions
    {
        public static IRabbitMqConsumer Start(this IRabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            return consumer.StartAsync().GetAwaiter().GetResult();
        }

        public static IRabbitMqConsumer Stop(this IRabbitMqConsumer consumer)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            return consumer.StopAsync().GetAwaiter().GetResult();
        }

        public static IRabbitMqConsumer RegisterBeforeStartAction(this IRabbitMqConsumer consumer,
            Action<IRabbitMqConsumer> action, int priority = int.MaxValue)
        {
            return consumer.RegisterBeforeStartAction((c, _) =>
            {
                action(c);
                return Task.CompletedTask;
            }, priority);
        }

        public static IRabbitMqConsumer WithQueueBinding(this IRabbitMqConsumer consumer,
            string exchangeName,
            string routingKey)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            async Task OnBeforeStart(IRabbitMqConsumer _, CancellationToken cancellationToken)
            {
                await consumer.Client.BindQueueAsync(exchangeName, routingKey, consumer.QueueName, cancellationToken)
                    .ConfigureAwait(false);
            }

            consumer.RegisterBeforeStartAction(OnBeforeStart, BeforeStartActionPriorities.BindQueue);

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

            async Task OnBeforeStart(IRabbitMqConsumer _, CancellationToken cancellationToken)
            {
                await consumer.Client.CreateQueueIfNotExistsAsync(queueName, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            consumer.RegisterBeforeStartAction(OnBeforeStart, BeforeStartActionPriorities.DeclareQueue);

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

            return consumer.WithDeclareQueue(consumer.QueueName, options);
        }

        public static IRabbitMqConsumer WithDeclareErrorQueue(this IRabbitMqConsumer consumer, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            var errorQueueName = consumer.Client.Options.NamingConventions.ErrorQueueName(consumer.QueueName);
            return consumer.WithDeclareQueue(errorQueueName, options);
        }

        public static IRabbitMqConsumer WithDeclareErrorQueue(this IRabbitMqConsumer consumer,
            Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithDeclareErrorQueue(consumer, options);
        }

        public static IRabbitMqConsumer WithDeclareExchange(this IRabbitMqConsumer consumer, string exchangeName,
            Action<ExchangeOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = ExchangeOptions.Default;
            optionsSetup(options);

            return consumer.WithDeclareExchange(exchangeName, options);
        }

        public static IRabbitMqConsumer WithDeclareExchange(this IRabbitMqConsumer consumer, string exchangeName,
            ExchangeOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            async Task OnBeforeStart(IRabbitMqConsumer _, CancellationToken cancellationToken)
            {
                await consumer.Client.CreateExchangeIfNotExistsAsync(exchangeName, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            consumer.RegisterBeforeStartAction(OnBeforeStart, BeforeStartActionPriorities.DeclareExchange);

            return consumer;
        }

        private static class BeforeStartActionPriorities
        {
            public static int DeclareQueue => 0;
            public static int DeclareExchange => 0;
            public static int BindQueue => 10;
        }
    }
}