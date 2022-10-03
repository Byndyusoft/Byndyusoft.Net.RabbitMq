using System.Collections.Generic;

namespace Byndyusoft.Messaging.RabbitMq
{
    public class RabbitMqMessageHeaders : Dictionary<string, object?>
    {
        public RabbitMqMessageHeaders()
        {
        }

        public RabbitMqMessageHeaders(IDictionary<string, object?> collection)
            : base(collection)
        {
        }

        public new object? this[string key]
        {
            set => base[key] = value;
            get
            {
                TryGetValue(key, out var value);
                return value;
            }
        }
    }
}