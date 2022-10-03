using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.RabbitMq.Topology;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IRabbitMqClient : IDisposable
    {
        RabbitMqClientCoreOptions Options { get; }

        #region Работа с сообщениями

        Task<ReceivedRabbitMqMessage?> GetMessageAsync(string queueName, CancellationToken cancellationToken = default);

        Task<ulong> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken = default);

        Task CompleteMessageAsync(ReceivedRabbitMqMessage message, ConsumeResult consumeResult,
            CancellationToken cancellationToken = default);

        Task PublishMessageAsync(RabbitMqMessage message, CancellationToken cancellationToken = default);

        IRabbitMqConsumer Subscribe(string queueName,
            ReceivedRabbitMqMessageHandler onMessage);

        Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken = default);

        #endregion

        #region Управление очередями и обменниками

        Task CreateQueueAsync(string queueName, QueueOptions options, CancellationToken cancellationToken = default);

        Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default);

        Task DeleteQueueAsync(string queueName,
            bool ifUnused = false,
            bool ifEmpty = false,
            CancellationToken cancellationToken = default);

        Task CreateExchangeAsync(string exchangeName,
            ExchangeOptions options,
            CancellationToken cancellationToken = default);

        Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default);

        Task DeleteExchangeAsync(string exchangeName,
            bool ifUnused = false,
            CancellationToken cancellationToken = default);

        Task BindQueueAsync(string exchangeName,
            string routingKey,
            string queueName,
            CancellationToken cancellationToken = default);

        #endregion
    }
}