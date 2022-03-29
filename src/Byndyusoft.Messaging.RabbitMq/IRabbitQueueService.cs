using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitQueueService : IQueueService
    {
        Task CreateQueueAsync(string queueName, QueueOptions options,
            CancellationToken cancellationToken = default);

        Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default);

        Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false,
            CancellationToken cancellationToken = default);

        Task CreateExchangeAsync(string exchangeName, ExchangeOptions options,
            CancellationToken cancellationToken = default);

        Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default);

        Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false,
            CancellationToken cancellationToken = default);

        Task BindQueueAsync(string? exchangeName, string routingKey, string queueName,
            CancellationToken cancellationToken = default);
    }
}