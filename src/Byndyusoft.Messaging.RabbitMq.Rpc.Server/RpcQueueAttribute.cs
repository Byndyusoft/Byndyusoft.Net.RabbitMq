using System;

namespace Byndyusoft.Messaging.RabbitMq.Rpc
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RpcQueueAttribute : Attribute
    {
        public RpcQueueAttribute(string queueName)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }

        public ushort PrefetchCount { get; set; }
    }
}