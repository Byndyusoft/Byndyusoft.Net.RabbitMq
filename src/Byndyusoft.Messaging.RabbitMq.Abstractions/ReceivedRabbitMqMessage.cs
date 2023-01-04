using System.Net.Http;
using System.Net.Http.Headers;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class ReceivedRabbitMqMessage : AsyncDisposable
    {
        private readonly string _consumerTag = default!;
        private readonly RabbitMqMessageHeaders _headers = new();
        private readonly RabbitMqMessageProperties _properties = new();
        private readonly string _queue = default!;
        private readonly string _routingKey = default!;
        private HttpContent? _content;

        public bool Persistent { get; init; }

        public ulong RetryCount { get; init; }

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
            init
            {
                _properties = Preconditions.CheckNotNull(value, nameof(Properties));
                UpdateContentProperties();
            }
        }

        public HttpContent Content
        {
            get => _content!;
            init
            {
                _content = Preconditions.CheckNotNull(value, nameof(Content));
                UpdateContentProperties();
            }
        }

        public RabbitMqMessageHeaders Headers
        {
            get => _headers;
            init => _headers = Preconditions.CheckNotNull(value, nameof(Headers));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            _content?.Dispose();
            _content = null;
        }

        private void UpdateContentProperties()
        {
            if (_content is null)
                return;

            if (_properties.ContentType is not null)
                _content.Headers.ContentType = new MediaTypeHeaderValue(_properties.ContentType);

            if (_properties.ContentEncoding is not null)
                _content.Headers.ContentEncoding.Add(_properties.ContentEncoding);
        }
    }
}