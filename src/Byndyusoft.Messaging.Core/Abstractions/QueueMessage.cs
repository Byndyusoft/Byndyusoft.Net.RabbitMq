using System.Net.Http;
using Byndyusoft.Messaging.Utils;

namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueMessage : Disposable
    {
        private HttpContent? _content;
        private string? _exchange;
        private QueueMessageHeaders _headers;
        private bool _mandatory;
        private bool _persistent;
        private QueueMessageProperties _properties;
        private string? _routingKey;

        public QueueMessage()
        {
            _content = new StringContent(string.Empty);
            _headers = new QueueMessageHeaders();
            _properties = new QueueMessageProperties();
        }

        public string? Exchange
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _exchange;
            }
            set
            {
                Preconditions.CheckNotDisposed(this);
                _exchange = value;
            }
        }

        public string? RoutingKey
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _routingKey;
            }
            set
            {
                Preconditions.CheckNotDisposed(this);
                _routingKey = value;
            }
        }

        public QueueMessageProperties Properties
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _properties;
            }
            set
            {
                Preconditions.CheckNotDisposed(this);
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
            set
            {
                Preconditions.CheckNotDisposed(this);
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
            set
            {
                Preconditions.CheckNotNull(value, nameof(Headers));
                _headers = value;
            }
        }

        public bool Persistent
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _persistent;
            }
            set
            {
                Preconditions.CheckNotDisposed(this);
                _persistent = value;
            }
        }

        public bool Mandatory
        {
            get
            {
                Preconditions.CheckNotDisposed(this);
                return _mandatory;
            }
            set
            {
                Preconditions.CheckNotDisposed(this);
                _mandatory = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _content?.Dispose();
                _content = null;
            }
        }
    }
}