using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqClient
    {
        Task PublishAsync(RabbitMqMessage message, CancellationToken cancellationToken = default);

        Task PublishBatchAsync(IEnumerable<RabbitMqMessage> message,
            CancellationToken cancellationToken = default);

        Task<RabbitMqConsumedMessage> GetAsync(string queueName, CancellationToken cancellationToken = default);

        IRabbitMqMessageConsumer Subscribe(string queueName, Func<RabbitMqConsumedMessage, AckResult> handler);

        Task AckAsync(RabbitMqConsumedMessage message);

        Task NackAsync(RabbitMqConsumedMessage message);
    }
}