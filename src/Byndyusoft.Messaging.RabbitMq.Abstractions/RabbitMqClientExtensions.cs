using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

        public static async Task PublishAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            Func<T, CancellationToken, ValueTask<HttpContent>> modelSerializer,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(modelSerializer, nameof(modelSerializer));

            var content = await modelSerializer(model, cancellationToken).ConfigureAwait(false);
            await using var message = new RabbitMqMessage();
            message.Content = content;
            message.Exchange = exchangeName;
            message.RoutingKey = routingKey;
            message.Persistent = true;
            message.Mandatory = true;
            await client.PublishMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public static Task PublishAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            Func<T, HttpContent> modelSerializer,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(modelSerializer, nameof(modelSerializer));

            return PublishAsync(client, exchangeName, routingKey, model, SerializeModel, cancellationToken);

            ValueTask<HttpContent> SerializeModel(T x, CancellationToken _) => new(modelSerializer(x));
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<T?>> modelDeserializer)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotNull(modelDeserializer, nameof(modelDeserializer));

            return client.Subscribe(queueName, OnMessage);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await modelDeserializer.Invoke(message, token).ConfigureAwait(false);
                return await onMessage(model, token).ConfigureAwait(false);
            }
        }

        public static IRabbitMqConsumer SubscribeAs<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task> onMessage,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<T?>> modelDeserializer)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotNull(modelDeserializer, nameof(modelDeserializer));

            return client.SubscribeAs(queueName, OnMessage, modelDeserializer);

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }
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