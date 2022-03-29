using EasyNetQ;

namespace Byndyusoft.Messaging.RabbitMq
{
    public interface IBusFactory
    {
        IBus CreateBus(ConnectionConfiguration connectionConfiguration);
    }
}