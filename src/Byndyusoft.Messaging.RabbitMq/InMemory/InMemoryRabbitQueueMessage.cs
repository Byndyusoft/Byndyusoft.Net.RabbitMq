using System;
using System.Net.Http;
using Byndyusoft.Messaging.Abstractions;
using Byndyusoft.Messaging.RabbitMq.Internal;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.RabbitMq.InMemory
{
    public class InMemoryRabbitQueueMessage : Disposable
    {
        public InMemoryRabbitQueueMessage(QueueMessage message)
        {
            Preconditions.CheckNotNull(message, nameof(message));

            Message = message;
            PublishedAt = DateTime.UtcNow;
            RetryCount = message.Headers.GetRetryCount() ?? 0;
            Content = RabbitMessageContent.Create(message.Content);
        }

        public DateTime PublishedAt { get; }

        public QueueMessage Message { get; }

        public HttpContent Content { get; }

        public bool IsConsuming => DeliveryTag is not null;

        public bool IsReady => DeliveryTag is null && IsExpired() == false;

        public ulong? DeliveryTag { get; private set; }

        public bool Redelivered { get; private set; }

        public long RetryCount { get; private set; }

        internal bool IsExpired(TimeSpan? expiration = null)
        {
            expiration ??= Message.Properties.Expiration;
            return expiration is not null && DateTime.UtcNow.Subtract(PublishedAt) >= expiration.Value;
        }

        internal void Requeue()
        {
            DeliveryTag = null;
            Redelivered = true;
        }

        internal void RetryTo(InMemoryRabbitQueue? queue)
        {
            if (queue is null)
                return;

            RetryCount += 1;
            Redelivered = false;
            queue.Add(this);
        }

        internal ConsumedQueueMessage Consume(string queueName, string? consumerTag)
        {
            Preconditions.Check(IsConsuming == false, "Can't consume already consuming message");

            DeliveryTag = (ulong) BitConverter.ToInt64(Guid.NewGuid().ToByteArray());

            return new ConsumedQueueMessage
            {
                Content = RabbitMessageContent.Create(Content),
                Queue = queueName,
                ConsumerTag = consumerTag ?? string.Empty,
                DeliveryTag = DeliveryTag.Value,
                Exchange = Message.Exchange,
                RoutingKey = Message.RoutingKey,
                Persistent = Message.Persistent,
                Redelivered = Redelivered,
                Properties = Message.Properties,
                Headers = Message.Headers,
                RetryCount = RetryCount
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) Message.Dispose();
        }
    }
}