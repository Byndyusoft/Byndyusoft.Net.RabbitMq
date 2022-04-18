using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Topology;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;
using Byndyusoft.Messaging.RabbitMq.Core.Messages;

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

        public static IRabbitMqConsumer WithExclusive(this IRabbitMqConsumer consumer,
            bool exclusive)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            consumer.Exclusive = exclusive;

            return consumer;
        }

        public static IRabbitMqConsumer WithCreatingSubscribeQueue(this IRabbitMqConsumer consumer, Action<QueueOptions>? optionsSetup = null)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var options = QueueOptions.Default;
            optionsSetup?.Invoke(options);

            consumer.OnStarting += async (_, cancellationToken) =>
            {
                await consumer.Client.CreateQueueIfNotExistsAsync(consumer.QueueName, options, cancellationToken)
                    .ConfigureAwait(false);
            };

            return consumer;
        }

        public static IRabbitMqConsumer WithSingleQueueRetry(this IRabbitMqConsumer consumer, TimeSpan delay, Action<QueueOptions>? optionsSetup = null)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var options = QueueOptions.Default;
            optionsSetup?.Invoke(options);

            return consumer.WithSingleQueueRetry(delay, options);
        }

        public static IRabbitMqConsumer WithSingleQueueRetry(this IRabbitMqConsumer consumer, TimeSpan delay, QueueOptions options)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));
            Preconditions.CheckNotNull(options, nameof(options));

            var retryQueueName = consumer.Client.Options.NamingConventions.RetryQueueName(consumer.QueueName);
            options = options
                .WithMessageTtl(delay)
                .WithDeadLetterExchange(null)
                .WithDeadLetterRoutingKey(consumer.QueueName);

            consumer.AddHandler(handler => 
            {
                async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
                {
                    try
                    {
                        Exception? handleException = null;
                        try
                        {
                            var result = await handler(message, cancellationToken);
                            if (result == ConsumeResult.Ack)
                                return ConsumeResult.Ack;
                        }
                        catch (Exception exception)
                        {
                            handleException = exception;
                        }
                        var retryMessage = RabbitMqMessageFactory.CreateRetryMessage(message, retryQueueName);
                        if(handleException != null)
                            retryMessage.Headers.SetException(handleException);

                        await consumer.Client.PublishMessageAsync(retryMessage, cancellationToken).ConfigureAwait(false);
                        return ConsumeResult.Ack;
                    }
                    catch (Exception exception)
                    {
                        //TODO передавать исключение с Error
                        return ConsumeResult.Error;
                    }
                }

                return OnMessage;
            });


            return consumer.WithQueue(retryQueueName, options);
        }

        public static IRabbitMqConsumer WithQueue(this IRabbitMqConsumer consumer, string queueName, Action<QueueOptions> optionsSetup)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return consumer.WithQueue(queueName, options);
        }
        
        public static IRabbitMqConsumer WithQueue(this IRabbitMqConsumer consumer, string queueName, QueueOptions options)
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

            var options = QueueOptions.Default;
            optionsSetup(options);

            return WithErrorQueue(consumer, options);
        }
    }
}