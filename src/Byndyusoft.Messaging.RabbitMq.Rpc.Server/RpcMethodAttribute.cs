using System;
using Byndyusoft.Messaging.RabbitMq.Utils;

namespace Byndyusoft.Messaging.RabbitMq.Rpc
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RpcMethodAttribute : Attribute
    {
        public RpcMethodAttribute(string queueNameOrKey)
        {
            QueueNameOrKey = Preconditions.CheckNotNull(queueNameOrKey, nameof(queueNameOrKey));
        }

        public string QueueNameOrKey { get; }

        public ushort PrefetchCount { get; set; }
    }
}