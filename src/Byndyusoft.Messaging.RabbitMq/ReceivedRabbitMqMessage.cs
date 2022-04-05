using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Json.Formatting;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class ReceivedRabbitMqMessage : Disposable
    {
        private readonly string _consumerTag = default!;
        private readonly HttpContent? _content;
        private readonly RabbitMqMessageHeaders _headers = new();
        private readonly bool _persistent;
        private readonly RabbitMqMessageProperties _properties = new();
        private readonly string _queue = default!;
        private readonly string _routingKey = default!;

        static ReceivedRabbitMqMessage()
        {
            MediaTypeFormatterCollection.Default.Add(new JsonMediaTypeFormatter());
        }

        public bool Persistent
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _persistent;
            }
            init => _persistent = value;
        }

        public long RetryCount { get; init; }

        /// <summary>
        ///     Consumer (subscription) identifier
        /// </summary>
        public string ConsumerTag
        {
            get => _consumerTag;
            init
            {
                Preconditions.CheckNotNull(value, nameof(ConsumerTag));
                _consumerTag = value;
            }
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
            init
            {
                Preconditions.CheckNotNull(value, nameof(RoutingKey));
                _routingKey = value;
            }
        }

        /// <summary>
        ///     Queue name used by the consumer
        /// </summary>
        public string Queue
        {
            get => _queue;
            init
            {
                Preconditions.CheckNotNull(value, nameof(Queue));
                _queue = value;
            }
        }

        public RabbitMqMessageProperties Properties
        {
            get => _properties;
            init
            {
                Preconditions.CheckNotNull(value, nameof(Properties));
                _properties = value;
            }
        }

        public HttpContent Content
        {
            get => _content!;
            init
            {
                Preconditions.CheckNotNull(value, nameof(Content));
                _content = value;
            }
        }

        public RabbitMqMessageHeaders Headers
        {
            get => _headers;
            init
            {
                Preconditions.CheckNotNull(value, nameof(Headers));
                _headers = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _content?.Dispose();
        }
    }
}