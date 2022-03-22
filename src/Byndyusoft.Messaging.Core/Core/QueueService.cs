using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Core
{
    public abstract class QueueService : Disposable, IQueueService
    {
        private IQueueServiceHandler _handler = default!;

        protected QueueService()
        {
        }

        protected QueueService(IQueueServiceHandler handler)
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            Handler = handler;
        }

        protected IQueueServiceHandler Handler
        {
            set
            {
                Preconditions.CheckNotNull(value, nameof(Handler));
                _handler = value;
            }
        }

        public async Task<ConsumedQueueMessage?> GetAsync(string queueName,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            var activity = QueueServiceActivitySource.StartGet(_handler.ConnectionConfiguration, queueName);
            return await QueueServiceActivitySource.ExecuteAsync(activity, async () =>
            {
                var message = await _handler.GetAsync(queueName, cancellationToken)
                    .ConfigureAwait(false);
                QueueServiceActivitySource.MessageGot(activity, message);
                return message;
            });
        }

        public async Task AckAsync(ConsumedQueueMessage message, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            var activity = QueueServiceActivitySource.StartAck(_handler.ConnectionConfiguration, message);
            await QueueServiceActivitySource.ExecuteAsync(activity, async () =>
            {
                await _handler.AckAsync(message, cancellationToken).ConfigureAwait(false);
                return 0;
            });
        }

        public async Task RejectAsync(ConsumedQueueMessage message, bool requeue = false,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            var activity = QueueServiceActivitySource.StartReject(_handler.ConnectionConfiguration, message, requeue);
            await QueueServiceActivitySource.ExecuteAsync(activity, async () =>
            {
                await _handler.RejectAsync(message, requeue, cancellationToken).ConfigureAwait(false);
                return 0;
            });
        }

        public async Task PublishAsync(QueueMessage message, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            SetPublishingMessageProperties(message);

            var activity = QueueServiceActivitySource.StartPublish(_handler.ConnectionConfiguration, message);
            await QueueServiceActivitySource.ExecuteAsync(activity, async () =>
            {
                await _handler.PublishAsync(message, cancellationToken).ConfigureAwait(false);
                return 0;
            });
        }

        public async Task PublishBatchAsync(IReadOnlyCollection<QueueMessage> messages,
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(messages, nameof(messages));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            foreach (var message in messages) SetPublishingMessageProperties(message);

            var activity = QueueServiceActivitySource.StartBatchPublish(_handler.ConnectionConfiguration, messages);
            await QueueServiceActivitySource.ExecuteAsync(activity, async () =>
            {
                await _handler.PublishBatchAsync(messages, cancellationToken).ConfigureAwait(false);
                return 0;
            });
        }

        public IQueueConsumer Subscribe(string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage, bool autoStart = true)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            var consumer = new QueueConsumer(_handler, queueName, onMessage);
            if (autoStart)
                consumer.StartAsync().GetAwaiter().GetResult();
            return consumer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handler.Dispose();
                _handler = null!;
            }

            base.Dispose(disposing);
        }

        private void SetPublishingMessageProperties(QueueMessage message)
        {
            message.Properties.ContentEncoding ??= message.Content.Headers.ContentEncoding?.FirstOrDefault();
            message.Properties.ContentType ??= message.Content.Headers.ContentType?.MediaType;
            message.Properties.Type ??= (message.Content as ObjectContent)?.ObjectType.FullName;
        }
    }
}