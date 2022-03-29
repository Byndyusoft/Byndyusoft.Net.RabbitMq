using System;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Topology;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public static class RabbitQueueServiceExtensions
    {
        public static async Task CreateQueueAsync(this IRabbitQueueService queueService, string queueName,
            Action<QueueOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = QueueOptions.Default;
            optionsSetup(options);

            await queueService.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> CreateQueueIfNotExistsAsync(this IRabbitQueueService queueService,
            string queueName, QueueOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(options, nameof(options));

            if (await queueService.QueueExistsAsync(queueName, cancellationToken).ConfigureAwait(false))
                return false;

            await queueService.CreateQueueAsync(queueName, options, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<bool> CreateQueueIfNotExistsAsync(this IRabbitQueueService queueService,
            string queueName, Action<QueueOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            await queueService.CreateQueueAsync(queueName, optionsSetup, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task CreateExchangeAsync(this IRabbitQueueService queueService, string exchangeName,
            Action<ExchangeOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            var options = ExchangeOptions.Default;
            optionsSetup(options);

            await queueService.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> CreateExchangeIfNotExistsAsync(this IRabbitQueueService queueService,
            string exchangeName, ExchangeOptions options,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(options, nameof(options));

            if (await queueService.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false))
                return false;

            await queueService.CreateExchangeAsync(exchangeName, options, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public static async Task<bool> CreateExchangeIfNotExistsAsync(this IRabbitQueueService queueService,
            string exchangeName, Action<ExchangeOptions> optionsSetup,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueService, nameof(queueService));
            Preconditions.CheckNotNull(exchangeName, nameof(exchangeName));
            Preconditions.CheckNotNull(optionsSetup, nameof(optionsSetup));

            if (await queueService.ExchangeExistsAsync(exchangeName, cancellationToken).ConfigureAwait(false))
                return false;

            await queueService.CreateExchangeAsync(exchangeName, optionsSetup, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}