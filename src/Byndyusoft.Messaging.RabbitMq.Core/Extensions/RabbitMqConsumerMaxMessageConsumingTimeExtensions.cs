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

            Task<ConsumeResult> OnMessage(ReceivedRabbitMqMessage message, CancellationToken cancellationToken)
            {
                using var cts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        new CancellationTokenSource(maxConsumingTime).Token);
                return onMessage(message, cts.Token);
            }
        }
    }
}