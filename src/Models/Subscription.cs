using Byndyusoft.AspNetCore.RabbitMq.Abstractions;

namespace Byndyusoft.AspNetCore.RabbitMq.Models
{
    public interface ISubscription
    {
        string RoutingKey { get; }

        IMessageHandler Handler { get; }
    }
}