using System.Net.Http;
using Byndyusoft.Messaging.RabbitMq.Abstractions.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public class ReceivedRabbitMqMessage : Disposable
    {
        private readonly string _consumerTag = default!;
        private readonly HttpContent? _content;
        private readonly RabbitMqMessageHeaders _headers = new();
        private readonly RabbitMqMessageProperties _properties = new();
        private readonly string _queue = default!;
        private readonly string _routingKey = default!;
        private readonly long _retryCount;

        public bool Persistent { get; init; }

        public long RetryCount
        {
            get => _retryCount;
            init
            {
                Preconditions.Check(_retryCount>=0, $"{nameof(RetryCount)} should be positive number");
                _retryCount = value;
            }
        }

        /// <summary>
        ///     Consumer (subscription) identifier
        /// </summary>
        public string ConsumerTag
        {
            get => _consumerTag;
            init => _consumerTag = Preconditions.CheckNotNull(value, nameof(ConsumerTag));
        }

        /// <summary>
        ///     Delivery identifier
        /// </summary>
        public ulong DeliveryTag { get; init; }

        /// <summary>
        ///     Set to `true` if this message was previously delivered and requeued
        /// </summary>
        public bool Redelivered { get; init; }

        /// <summary>
        ///     Exchange which routed this message
        /// </summary>
        public string? Exchange { get; init; }

        /// <summary>
        ///     Routing key used by the publisher
        /// </summary>
        public string RoutingKey
        {
            get => _routingKey;
            init => _routingKey = Preconditions.CheckNotNull(value, nameof(RoutingKey));
        }

        /// <summary>
        ///     Queue name used by the consumer
        /// </summary>
        public string Queue
        {
            get => _queue;
            init => _queue = Preconditions.CheckNotNull(value, nameof(Queue));
        }

        public RabbitMqMessageProperties Properties
        {
            get => _properties;
            init => _properties = Preconditions.CheckNotNull(value, nameof(Properties));
        }

        public HttpContent Content
        {
            get => _content!;
            init => _content = Preconditions.CheckNotNull(value, nameof(Content));
        }

        public RabbitMqMessageHeaders Headers
        {
            get => _headers;
            init => _headers = Preconditions.CheckNotNull(value, nameof(Headers));
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();

            _content?.Dispose();
        }
    }
}