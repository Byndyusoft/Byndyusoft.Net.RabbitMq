using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public static class QueueServiceJsonExtensions
    {
        public static IQueueConsumer SubscribeAsJson<T>(this IQueueService queueService, string queueName,
            Func<T?, CancellationToken, Task> onMessage, JsonSerializerOptions? options = null)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
                await onMessage(model, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return queueService.Subscribe(queueName, OnMessage);
        }

        public static async Task PublishAsJsonAsync<T>(this IQueueService queueService, string? exchangeName,
            string routingKey, T model, JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));

            var message = new QueueMessage
            {
                Content = JsonContent.Create(model, options: options),
                Exchange = exchangeName,
                RoutingKey = routingKey,
                Persistent = true,
                Mandatory = true
            };
            await queueService.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public static Task PublishAsJsonAsync<T>(this IQueueService queueService, string? exchangeName,
            string routingKey, T model,
            CancellationToken cancellationToken = default)
        {
            return queueService.PublishAsJsonAsync(exchangeName, routingKey, model, null, cancellationToken);
        }
    }
}