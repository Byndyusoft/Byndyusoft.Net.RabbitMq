using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Core;
using Byndyusoft.Messaging.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitQueueService : QueueService, IRabbitQueueService
    {
        private readonly IRabbitQueueServiceHandler _handler;

        public RabbitQueueService(string connectionString)
            : base(new RabbitQueueServiceHandler(connectionString), true)
        {
        }

        public RabbitQueueService(IRabbitQueueServiceHandler handler)
            : base(handler)
        {
            _handler = handler;
        }

        public override IQueueConsumer Subscribe(string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage, bool autoStart = true)
        {
            var consumer = base.Subscribe(queueName, onMessage, false);
            consumer.OnStarting(async (_, handler, cancellationToken) =>
            {
                await handler.CreateQueueIfNotExistsAsync(queueName, QueueOptions.Default, cancellationToken)
                    .ConfigureAwait(false);
            });

            try
            {
                if (autoStart)
                    consumer.Start();
                return consumer;
            }
            catch
            {
                consumer.Dispose();
                throw;
            }
        }

        public async Task CreateQueueAsync(string queueName, QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> QueueExistsAsync(string queueName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            return await _handler.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteQueueAsync(string queueName, bool ifUnused = false, bool ifEmpty = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.DeleteQueueAsync(queueName, ifUnused, ifEmpty, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateExchangeAsync(string exchangeName, ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));

            await _handler.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ExchangeExistsAsync(string exchangeName, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            return await _handler.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteExchangeAsync(string exchangeName, bool ifUnused = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));

            await _handler.DeleteExchangeAsync(exchangeName, ifUnused, cancellationToken).ConfigureAwait(false);
        }

        public async Task BindQueueAsync(string? exchangeName, string routingKey, string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(routingKey, nameof(routingKey));
            Preconditions.CheckNotNull(queueName, nameof(queueName));

            await _handler.BindQueueAsync(exchangeName, routingKey, queueName, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}