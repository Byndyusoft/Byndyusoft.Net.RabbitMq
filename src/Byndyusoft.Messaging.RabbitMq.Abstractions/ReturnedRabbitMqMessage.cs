using System.Net.Http;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class ReturnedRabbitMqMessage : AsyncDisposable
    {
        private readonly RabbitMqMessageHeaders _headers = new();
        private readonly RabbitMqMessageProperties _properties = new();
        private readonly string _returnReason = default!;
        private readonly string _routingKey = default!;
        private HttpContent? _content;

        public string ReturnReason
        {
            get => _returnReason;
            init => _returnReason = Preconditions.CheckNotNull(value, nameof(ReturnReason));
        }

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing == false) return;

            _content?.Dispose();
            _content = null;
        }
    }
}