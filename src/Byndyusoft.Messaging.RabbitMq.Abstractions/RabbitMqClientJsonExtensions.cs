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
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.Subscribe(queueName, OnMessage);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
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
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.Subscribe(exchangeName, routingKey, OnMessage);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            string consumerName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.Subscribe(exchangeName, routingKey, consumerName, OnMessage);

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                var result = await onMessage(model, token).ConfigureAwait(false);
                return result;
            }
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
           Preconditions.CheckNotNull(onMessage, nameof(onMessage));

           return client.SubscribeAsJson<T>(exchangeName, routingKey, OnMessage, options);

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string exchangeName,
            string routingKey,
            string consumerName,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.SubscribeAsJson<T>(exchangeName, routingKey, consumerName, OnMessage, options);

            async Task<ConsumeResult> OnMessage(T? message, CancellationToken token)
            {
                await onMessage(message, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }
        }

        public static IRabbitMqConsumer SubscribeRpcAsJson<TRequest, TResponse>(
            this IRabbitMqClient client,
            string queueName,
            Func<TRequest?, CancellationToken, Task<TResponse>> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.SubscribeRpc(queueName, OnMessage);

            async Task<RpcResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken token)
            {
                var request = await message.Content.ReadFromJsonAsync<TRequest>(options, token)
                    .ConfigureAwait(false);
                var response = await onMessage(request, token)
                    .ConfigureAwait(false);
                return RpcResult.Success(JsonContent.Create(response, options: options));
            }
        }

        public static async Task<TResponse?> MakeRpcAsJson<TRequest, TResponse>(
            this IRabbitMqClient client,
            string queueName,
            TRequest request,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var requestMessage = new RabbitMqMessage
            {
                Mandatory = true,
                Content = JsonContent.Create(request, options: options),
                RoutingKey = queueName
            };
            var responseMessage = await client.MakeRpc(requestMessage, cancellationToken)
                .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync<TResponse>(options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}