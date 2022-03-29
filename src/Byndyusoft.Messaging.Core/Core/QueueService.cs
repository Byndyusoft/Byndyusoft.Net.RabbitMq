using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Core
{
    public abstract class QueueService : Disposable, IQueueService
    {
        private readonly bool _disposeHandler;
        private readonly QueueServiceOptions _options = default!;
        private IQueueServiceHandler _handler = default!;

        private QueueService()
        {
        }

        protected QueueService(IQueueServiceHandler handler, bool disposeHandler = false)
            : this()
        {
            Preconditions.CheckNotNull(handler, nameof(handler));

            _handler = handler;
            _disposeHandler = disposeHandler;
            _options = handler.Options;
        }

        public QueueServiceOptions Options
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _options;
            }
        }

        public virtual async Task<ConsumedQueueMessage?> GetAsync(string queueName,
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
                SetConsumedMessageProperties(message);
                QueueServiceActivitySource.MessageGot(activity, message);
                return message;
            });
        }

        public virtual async Task AckAsync(ConsumedQueueMessage message, CancellationToken cancellationToken = default)
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

        public virtual async Task RejectAsync(ConsumedQueueMessage message, bool requeue = false,
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

        public virtual async Task PublishAsync(QueueMessage message, CancellationToken cancellationToken = default)
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

        public virtual async Task PublishBatchAsync(IReadOnlyCollection<QueueMessage> messages,
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

        public virtual IQueueConsumer Subscribe(string queueName,
            Func<ConsumedQueueMessage, CancellationToken, Task<ConsumeResult>> onMessage, bool autoStart = true)
        {
            Preconditions.CheckNotNull(queueName, nameof(queueName));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));
            Preconditions.CheckNotDisposed(this);
            Preconditions.CheckNotNull(_handler, nameof(QueueService), "Handler should be provided");

            async Task<ConsumeResult> OnMessage(ConsumedQueueMessage message, CancellationToken cancellationToken)
            {
                SetConsumedMessageProperties(message);
                return await onMessage(message, cancellationToken).ConfigureAwait(false);
            }

            var consumer = new QueueConsumer(this, _handler, queueName, OnMessage);
            try
            {
                if (autoStart) consumer.Start();
                return consumer;
            }
            catch
            {
                consumer.Dispose();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (_disposeHandler)
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

            if (message.Properties.Type is null)
            {
                var objectType = (message.Content as ObjectContent)?.ObjectType ??
                                 (message.Content as JsonContent)?.ObjectType;
                if (objectType is not null) message.Properties.Type = $"{objectType.Name}, {objectType.Namespace}";
            }

            message.Properties.Type ??= (message.Content as ObjectContent)?.ObjectType.FullName;
            message.Properties.AppId ??= _options.ApplicationName;
        }

        private void SetConsumedMessageProperties(ConsumedQueueMessage? message)
        {
            if (message is null)
                return;

            var properties = message.Properties;
            var content = message.Content;

            if (properties.ContentType is not null)
                content.Headers.ContentType = new MediaTypeHeaderValue(properties.ContentType);

            if (properties.ContentEncoding is not null) content.Headers.ContentEncoding.Add(properties.ContentEncoding);
        }
    }
}