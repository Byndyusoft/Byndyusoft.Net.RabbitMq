using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.Abstractions
{
    public interface IQueueService : IDisposable
    {
        QueueServiceOptions Options { get; }

        Task<ConsumedQueueMessage?> GetAsync(string queueName, CancellationToken cancellationToken = default);

        Task AckAsync(ConsumedQueueMessage message, CancellationToken cancellationToken = default);

        Task RejectAsync(ConsumedQueueMessage message, bool requeue = false,
            CancellationToken cancellationToken = default);

        Task PublishAsync(QueueMessage message, CancellationToken cancellationToken = default);

        Task PublishBatchAsync(IReadOnlyCollection<QueueMessage> messages,
            CancellationToken cancellationToken = default);

        IQueueConsumer Subscribe(string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage);
    }
}