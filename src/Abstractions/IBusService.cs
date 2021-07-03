using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Abstractions
{
    public interface IBusService
    {
        IBus CreateBus(string connectionString);
    }
}