using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Json.Formatting;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public class ConsumedQueueMessage : Disposable
    {
        private readonly string _consumerTag = default!;
        private readonly HttpContent? _content;
        private readonly ulong _deliveryTag;
        private readonly string? _exchange;
        private readonly QueueMessageHeaders _headers = new();
        private readonly QueueMessageProperties _properties = new();
        private readonly string _queue = default!;
        private readonly bool _redelivered;
        private readonly int _retryCount;
        private readonly string _routingKey = default!;

        static ConsumedQueueMessage()
        {
            MediaTypeFormatterCollection.Default.Add(new JsonMediaTypeFormatter());
        }

        public int RetryCount
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _retryCount;
            }
            init => _retryCount = value;
        }


        /// <summary>
        ///     Consumer (subscription) identifier
        /// </summary>
        public string ConsumerTag
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _consumerTag;
            }
            init
            {
                Preconditions.CheckNotNull(value, nameof(ConsumerTag));
                _consumerTag = value;
            }
        }

        /// <summary>
        ///     Delivery identifier
        /// </summary>
        public ulong DeliveryTag
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _deliveryTag;
            }
            init => _deliveryTag = value;
        }

        /// <summary>
        ///     Set to `true` if this message was previously delivered and requeued
        /// </summary>
        public bool Redelivered
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _redelivered;
            }
            init => _redelivered = value;
        }

        /// <summary>
        ///     Exchange which routed this message
        /// </summary>
        public string? Exchange
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _exchange;
            }
            init => _exchange = value;
        }

        /// <summary>
        ///     Routing key used by the publisher
        /// </summary>
        public string RoutingKey
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _routingKey;
            }
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
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _queue;
            }
            init
            {
                Preconditions.CheckNotNull(value, nameof(Queue));
                _queue = value;
            }
        }

        public QueueMessageProperties Properties
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _properties;
            }
            init
            {
                Preconditions.CheckNotNull(value, nameof(Properties));
                _properties = value;
            }
        }

        public HttpContent Content
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _content!;
            }
            init
            {
                Preconditions.CheckNotNull(value, nameof(Content));
                _content = value;
            }
        }

        public QueueMessageHeaders Headers
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _headers;
            }
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