using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    internal sealed class BusService : IBusService
    {
        public IBus CreateBus(string connectionString)
        {
            return RabbitHutch.CreateBus(connectionString);
        }
    }
}