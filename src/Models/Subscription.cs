using Byndyusoft.Net.RabbitMq.Abstractions;

namespace Byndyusoft.Net.RabbitMq.Models
{
    public interface ISubscription
    {
        string RoutingKey { get; }

        IMessageHandler Handler { get; }
    }
}