using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq.Abstractions
{
    public interface IBusFactory
    {
        IBus CreateBus(ConnectionConfiguration connectionConfiguration);
    }
}