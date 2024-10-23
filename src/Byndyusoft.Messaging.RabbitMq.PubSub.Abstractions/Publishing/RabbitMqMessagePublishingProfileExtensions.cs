namespace Byndyusoft.Messaging.RabbitMq.PubSub.Publishing
{
    using System;
    using System.Reflection;

    public static class RabbitMqMessagePublishingProfileExtensions
    {
        public static string? GetExchange(this Type producerType)
            => producerType.GetCustomAttribute<RabbitMqMessagePublishingProfileAttribute>(false)!.Exchange;

        public static string GetRoutingKey(this Type producerType)
            => producerType.GetCustomAttribute<RabbitMqMessagePublishingProfileAttribute>(false)!.RoutingKey;
    }
}