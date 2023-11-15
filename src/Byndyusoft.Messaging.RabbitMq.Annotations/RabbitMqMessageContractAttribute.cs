using System;

namespace Byndyusoft.Messaging.RabbitMq.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class RabbitMqMessageContractAttribute : Attribute
    {
        public string? RoutingKey { get; set; }

        public string? Exchange { get; set; }
    }
}
