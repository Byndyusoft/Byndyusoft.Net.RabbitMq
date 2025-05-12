// ReSharper disable CheckNamespace

using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitMqConsumerMaxMessageConsumingTimeExtensions
    {
        public static IRabbitMqConsumer WithMaxMessageConsumingTime(
            this IRabbitMqConsumer consumer,
            TimeSpan maxConsumingTime)
        {
            Preconditions.CheckNotNull(consumer, nameof(consumer));

            var onMessage = consumer.OnMessage;

            consumer.OnMessage = OnMessage;
            return consumer;

            async Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
            {
                using var timeoutTcs = 
                    new CancellationTokenSource(maxConsumingTime);
                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTcs.Token);
                return await onMessage(message, cts.Token);
            }
        }
    }
}