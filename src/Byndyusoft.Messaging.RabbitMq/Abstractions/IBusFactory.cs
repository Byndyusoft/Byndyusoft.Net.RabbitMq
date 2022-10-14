using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public interface IBusFactory
    {
        IBus CreateBus(RabbitMqClientOptions options, ConnectionConfiguration connectionConfiguration);
    }
}