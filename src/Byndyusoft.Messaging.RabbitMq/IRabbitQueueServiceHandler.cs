using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitQueueServiceHandler : IQueueServiceHandler
    {
        Task CreateQueueAsync(string queueName, QueueOptions options, CancellationToken cancellationToken);

        Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken);

        Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty, CancellationToken cancellationToken);

        Task<ulong> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken = default);

        Task CreateExchangeAsync(string exchangeName, ExchangeOptions options, CancellationToken cancellationToken);

        Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken);

        Task DeleteExchangeAsync(string exchangeName, bool ifUnused, CancellationToken cancellationToken);

        Task BindQueueAsync(string exchangeName, string routingKey, string queueName,
            CancellationToken cancellationToken);
    }
}