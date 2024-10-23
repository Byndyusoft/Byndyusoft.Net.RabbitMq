namespace Byndyusoft.Messaging.RabbitMq.PubSub.Publishing
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class RabbitMqMessagePublishingProfileAttribute : Attribute
    {
        public string RoutingKey { get; }

        public string? Exchange { get; set; }

        public RabbitMqMessagePublishingProfileAttribute(string routingKey)
        {
            RoutingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
        }
    }
}