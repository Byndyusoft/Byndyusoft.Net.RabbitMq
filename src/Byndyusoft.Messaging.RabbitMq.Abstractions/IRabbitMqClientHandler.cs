using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqClientHandler : IDisposable, IRabbitMqEndpointContainer
    {
        #region Работа с сообщениями

        Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName, CancellationToken cancellationToken);

        Task<ulong> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken);

        Task AckMessageAsync(ReceivedRabbitMqMessage message, CancellationToken cancellationToken);

        Task RejectMessageAsync(ReceivedRabbitMqMessage message, bool requeue, CancellationToken cancellationToken);

        Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken);

        Task<IDisposable> StartConsumeAsync(string queueName,
            bool? exclusive,
            ushort? prefetchCount,
            Func<ReceivedRabbitMqMessage, CancellationToken, Task<HandlerConsumeResult>> onMessage,
            CancellationToken cancellationToken);

        event ReturnedRabbitMqMessageHandler MessageReturned;

        #endregion

        #region Управлением очередями и обменниками

        Task CreateQueueAsync(string queueName, QueueOptions options, CancellationToken cancellationToken);

        Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken);

        Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty, CancellationToken cancellationToken);

        Task CreateExchangeAsync(string exchangeName, ExchangeOptions options, CancellationToken cancellationToken);

        Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken);

        Task DeleteExchangeAsync(string exchangeName, bool ifUnused, CancellationToken cancellationToken);

        Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken);

        Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

        #endregion
    }
}