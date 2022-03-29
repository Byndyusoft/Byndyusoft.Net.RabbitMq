using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;

namespace Byndyusoft.Messaging.Core
{
    public static class QueueServiceExtensions
    {
        public static IQueueConsumer Subscribe(this IQueueService queueService, string queueName,
            IQueueMessageHandler handler, bool autoStart = true)
        {
            return queueService.Subscribe(queueName, handler.HandleAsync, autoStart);
        }

        public static IQueueConsumer Subscribe(this IQueueService queueService, string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task> onMessage, bool autoStart = true)
        {
            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken token)
            {
                await onMessage(message, token);
                return ConsumeResult.Ack;
            }

            return queueService.Subscribe(queueName, OnMessage, autoStart);
        }
    }
}