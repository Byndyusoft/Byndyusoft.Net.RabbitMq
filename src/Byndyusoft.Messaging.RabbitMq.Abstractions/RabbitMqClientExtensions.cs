using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Topology;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientExtensions
    {
        public static async Task<bool> PullMessageAsync(
            this IRabbitMqClient client,
            string queueName,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<ConsumeResult>> onMessage,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            await using var message = await client.GetMessageAsync(queueName, cancellationToken)
                .ConfigureAwait(false);
            if (message is null)
                return false;

            try
            {
                var consumeResult = await onMessage(message, cancellationToken)
                    .ConfigureAwait(false);
                await client.CompleteMessageAsync(message, consumeResult, cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                await client.CompleteMessageAsync(message, ConsumeResult.Error(e), cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }
        }

        public static IRabbitMqConsumer Subscribe(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            ReceivedRabbitMqMessageHandler onMessage)
        {
            return Subscribe(client, exchangeName, routingKey, client.Options.ApplicationName, onMessage);
        }

        public static IRabbitMqConsumer Subscribe(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            string consumerName,
            ReceivedRabbitMqMessageHandler onMessage)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(consumerName, nameof(consumerName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            var queueName = client.Options.NamingConventions.QueueName(exchangeName, routingKey, consumerName);

            return client.Subscribe(queueName, onMessage)
                .WithSubscribingQueueBinding(exchangeName, routingKey);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadAsAsync<T>(token).ConfigureAwait(false);
                return await onMessage(model, token).ConfigureAwait(false);
            }

            return client.Subscribe(queueName, OnMessage);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task> onMessage)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return client.SubscribeAs<T>(queueName, OnMessage);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            Func<T?, CancellationToken, Task> onMessage)
        {
            return SubscribeAs(client, exchangeName, routingKey, client.Options.ApplicationName, onMessage);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            return SubscribeAs(client, exchangeName, routingKey, client.Options.ApplicationName, onMessage);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            string consumerName,
            Func<T?, CancellationToken, Task> onMessage)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(consumerName, nameof(consumerName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            
            var queueName = client.Options.NamingConventions.QueueName(exchangeName, routingKey, consumerName);

            return client.SubscribeAs(queueName, onMessage)
                .WithSubscribingQueueBinding(exchangeName, routingKey);
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            string consumerName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(consumerName, nameof(consumerName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            var queueName = client.Options.NamingConventions.QueueName(exchangeName, routingKey, consumerName);

            return client.SubscribeAs(queueName, onMessage)
                .WithSubscribingQueueBinding(exchangeName, routingKey);
        }

        public static async Task CreateQueueAsync(this IRabbitMqClient client,
            string queueName,
            Action<QueueOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            await client.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> CreateQueueIfNotExistsAsync(this IRabbitMqClient client,
            string queueName,
            QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            if (await client.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false))
                return false;

            await client.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<bool> CreateQueueIfNotExistsAsync(this IRabbitMqClient client,
            string queueName,
            Action<QueueOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            return await client.CreateQueueIfNotExistsAsync(queueName, options, cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task CreateExchangeAsync(this IRabbitMqClient client,
            string exchangeName,
            Action<ExchangeOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = ExchangeOptions.Default;
            optionsSetup(options);

            await client.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> CreateExchangeIfNotExistsAsync(this IRabbitMqClient client,
            string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));

            if (await client.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false))
                return false;

            await client.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<bool> CreateExchangeIfNotExistsAsync(this IRabbitMqClient client,
            string exchangeName,
            Action<ExchangeOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = ExchangeOptions.Default;
            optionsSetup(options);

            return await client.CreateExchangeIfNotExistsAsync(exchangeName, options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}