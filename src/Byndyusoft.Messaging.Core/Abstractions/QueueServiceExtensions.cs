using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public static class QueueServiceExtensions
    {
        public static IQueueConsumer Subscribe(this IQueueService queueService, string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task> onMessage)
        {
            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken token)
            {
                await onMessage(message, token);
                return ConsumeResult.Ack;
            }

            return queueService.Subscribe(queueName, OnMessage);
        }

        public static IQueueConsumer SubscribeAs<T>(this IQueueService queueService, string queueName,
            Func<T?, CancellationToken, Task<ConsumeResult>> onMessage)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken token)
            {
                var model = await message.Content.ReadAsAsync<T>(token).ConfigureAwait(false);
                return await onMessage(model, token).ConfigureAwait(false);
            }

            return queueService.Subscribe(queueName, OnMessage);
        }

        public static IQueueConsumer SubscribeAs<T>(this IQueueService queueService, string queueName,
            Func<T?, CancellationToken, Task> onMessage)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            async Task<ConsumeResult> OnMessage(T? model, CancellationToken token)
            {
                await onMessage(model, token).ConfigureAwait(false);
                return ConsumeResult.Ack;
            }

            return queueService.SubscribeAs<T>(queueName, OnMessage);
        }
    }
}