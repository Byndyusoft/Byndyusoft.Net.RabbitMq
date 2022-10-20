using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientJsonExtensions
    {
        public static async Task PublishAsJsonAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            var message = new RabbitMqMessage
            {
                Content = JsonContent.Create(model, options: options),
                Exchange = exchangeName,
                RoutingKey = routingKey,
                Persistent = true,
                Mandatory = true
            };
            await client.PublishMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public static Task PublishAsJsonAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            CancellationToken cancellationToken = default)
        {
            return PublishAsJsonAsync(client, exchangeName, routingKey, model, null, cancellationToken);
        }
        
        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }

            return client.Subscribe(queueName, OnMessage);
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return client.SubscribeAsJson<T>(queueName, OnMessage, options);
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }

            return client.Subscribe(exchangeName, routingKey, OnMessage);
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return client.SubscribeAsJson<T>(exchangeName, routingKey, OnMessage, options);
        }
    }
}