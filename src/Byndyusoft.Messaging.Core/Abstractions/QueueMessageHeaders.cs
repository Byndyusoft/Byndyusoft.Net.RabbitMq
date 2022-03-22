using System.Collections.Generic;

namespace Byndyusoft.Messaging.Abstractions
{
    public class QueueMessageHeaders : Dictionary<string, object?>
    {
        public QueueMessageHeaders()
        {
        }

        public QueueMessageHeaders(IDictionary<string, object?> collection)
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