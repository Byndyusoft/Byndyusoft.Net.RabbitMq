using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqClientJsonExtensions
    {
        public static Task PublishAsJsonAsync<T>(this IRabbitMqClient client,
            string? exchangeName,
            string routingKey,
            T model,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(client, nameof(client));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            return client.PublishAsync(exchangeName, routingKey, model, ModelSerializer, cancellationToken);

            HttpContent ModelSerializer(T x) => JsonContent.Create(x, options: options);
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

            return client.SubscribeAs(queueName, onMessage, ModelDeserializer);

            async Task<T?> ModelDeserializer(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
            {
                return await message.Content.ReadFromJsonAsync<T>(options, cancellationToken).ConfigureAwait(false);
            }
        }

        public static IRabbitMqConsumer SubscribeAsJson<T>(this IRabbitMqClient client,
            string queueName,
            Func<T?, CancellationToken, Task> onMessage,
            JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return client.SubscribeAsJson<T>(queueName, OnMessage, options);

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
            await using var requestMessage = new RabbitMqMessage();
            requestMessage.Mandatory = true;
            requestMessage.Content = JsonContent.Create(request, options: options);
            requestMessage.RoutingKey = queueName;
            var responseMessage = await client.MakeRpc(requestMessage, cancellationToken)
                .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync<TResponse>(options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}