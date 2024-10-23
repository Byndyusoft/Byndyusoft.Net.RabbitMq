namespace Byndyusoft.Messaging.RabbitMq.PubSub.Subscribing
{
    using System;
    using System.Reflection;

    public static class RabbitMqMessageHandlerTypeExtensions
    {
        public static string GetQueueName(this Type producerType)
            => producerType.GetCustomAttribute<RabbitMqMessageHandlerAttribute>(false)!.QueueName;
    }
}