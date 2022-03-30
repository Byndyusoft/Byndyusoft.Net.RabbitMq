using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Byndyusoft.Messaging.Abstractions
{
    public interface IQueueServiceHandler : IDisposable, IQueueServiceEndpointContainer
    {
        QueueServiceOptions Options { get; }

        Task<ConsumedQueueMessage?> GetAsync(string queueName, CancellationToken cancellationToken);

        Task AckAsync(ConsumedQueueMessage message, CancellationToken cancellationToken);

        Task RejectAsync(ConsumedQueueMessage message, bool requeue, CancellationToken cancellationToken);

        Task PublishAsync(QueueMessage message, CancellationToken cancellationToken);

        Task PublishBatchAsync(IReadOnlyCollection<QueueMessage> messages, CancellationToken cancellationToken);

        IDisposable Consume(string queueName, bool? exclusive, ushort? prefetchCount,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage);
    }
}