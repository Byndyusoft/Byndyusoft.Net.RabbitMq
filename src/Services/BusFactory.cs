using Byndyusoft.Net.RabbitMq.Abstractions;
using EasyNetQ;

namespace Byndyusoft.Net.RabbitMq.Services
{
    internal sealed class BusFactory : IBusFactory
    {
        public IBus CreateBus(string connectionString)
        {
            return RabbitHutch.CreateBus(connectionString);
        }
    }
}