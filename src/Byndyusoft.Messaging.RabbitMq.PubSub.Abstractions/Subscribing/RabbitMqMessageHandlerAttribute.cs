namespace Byndyusoft.Messaging.RabbitMq.PubSub.Subscribing
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class RabbitMqMessageHandlerAttribute : Attribute
    {
        public string QueueName { get; }

        public RabbitMqMessageHandlerAttribute(string queueName)
        {
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        }
    }
}